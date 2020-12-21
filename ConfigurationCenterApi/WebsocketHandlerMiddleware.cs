using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConfigurationCenterApi
{
    public class WebsocketHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebsocketHandlerMiddleware> _logger;
        private static ConnectionManager _connectionManager;
        private static int _socketCounter = 0;
        public static CancellationTokenSource SocketLoopTokenSource = new CancellationTokenSource();
        private static CancellationTokenRegistration _appShutdownRegistration;

        public WebsocketHandlerMiddleware(RequestDelegate next, ILogger<WebsocketHandlerMiddleware> logger, IHostApplicationLifetime hostLifetime)
        {
            _next = next;
            _logger = logger;
            _connectionManager = ConnectionManager.Instance;
            //only register on first instantiation
            if (_appShutdownRegistration.Token.Equals(CancellationToken.None))
            {
                _appShutdownRegistration = hostLifetime.ApplicationStopping.Register(ApplicationShutdownHandler);
            }
        }

        public static async void ApplicationShutdownHandler()
        {
            await _connectionManager.CloseAllSocketsAsync(() =>
            {
                SocketLoopTokenSource.Cancel();
            });

        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path == "/ws")
            {
                if (httpContext.WebSockets.IsWebSocketRequest)
                {
                    var appId = httpContext.Request.Headers["appid"];
                    var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                    var clientInfo = new WebsocketClientInfo()
                    {
                        SocketId = Interlocked.Increment(ref _socketCounter),
                        Client = webSocket,
                        AppId = appId
                    };
                    _connectionManager.AddClient(clientInfo.SocketId, clientInfo);
                    _logger.LogInformation("client:{0} connected", clientInfo.SocketId);

                    try
                    {
                        await Handle(clientInfo);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("websocket error!,client id:{0},error msg:{1}", clientInfo.SocketId,
                            e.Message);

                    }
                }
                else
                {
                    httpContext.Response.StatusCode = 404;
                }
            }
            else
            {
                await _next(httpContext);
            }
        }

        private async Task Handle(WebsocketClientInfo client)
        {
            var socket = client.Client;
            var loopToken = SocketLoopTokenSource.Token;
            try
            {
                var buffer = WebSocket.CreateServerBuffer(1024 * 4);
                while (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted &&
                       !loopToken.IsCancellationRequested)
                {
                    var receiveResult = await socket.ReceiveAsync(buffer, loopToken);
                    if (!loopToken.IsCancellationRequested)
                    {
                        if (socket.State == WebSocketState.CloseReceived &&
                            receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogInformation("Socket: {0},Acknowledging Close frame received from client",
                                client.SocketId);
                            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close frame",
                                CancellationToken.None);
                        }

                        if (socket.State == WebSocketState.Open &&
                            receiveResult.MessageType == WebSocketMessageType.Text)
                        {
                            var msg = Encoding.UTF8.GetString(buffer);
                            _logger.LogInformation("received msg:{0}", msg);
                            var response = Encoding.UTF8.GetBytes("ok");
                            await socket.SendAsync(new ArraySegment<byte>(response, 0, response.Length),
                                WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }

                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == loopToken)
            {
                _logger.LogInformation("websocket was cancelled");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            finally
            {
                _logger.LogInformation("Socket {0}: Ended loop in state: {1}", client.SocketId, socket.State);
                if (socket.State != WebSocketState.Closed)
                {
                    socket.Abort();
                }

                if (_connectionManager.RemoveClient(client.SocketId))
                {
                    _logger.LogInformation("Socket {0}:removed", client.SocketId);
                    socket.Dispose();
                }
            }

        }
    }
}