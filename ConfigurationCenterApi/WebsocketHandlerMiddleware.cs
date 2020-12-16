using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ConfigurationCenterApi
{
    public class WebsocketHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebsocketHandlerMiddleware> _logger;
        private readonly ConnectionManager _connectionManager;

        public WebsocketHandlerMiddleware(RequestDelegate next, ILogger<WebsocketHandlerMiddleware> logger, ConnectionManager connectionManager)
        {
            _next = next;
            _logger = logger;
            _connectionManager = connectionManager;
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
                        Id = Guid.NewGuid().ToString(),
                        Client = webSocket,
                        AppId = appId
                    };
                    _connectionManager.AddClient(clientInfo);
                    _logger.LogInformation("client:{0} connected", clientInfo.Id);

                    try
                    {
                        await Handle(clientInfo);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("websocket error!,client id:{0},error msg:{1}", clientInfo.Id,e.Message);
                        await _connectionManager.RemoveClient(clientInfo, WebSocketCloseStatus.Empty, e.Message);
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

        private async Task Handle(WebsocketClientInfo webSocketInfo)
        {
            var buffer = new byte[1024 * 2];
            WebSocketReceiveResult result;
            do
            {
                result = await webSocketInfo.Client.ReceiveAsync(new ArraySegment<byte>(buffer),
                    CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text && !result.CloseStatus.HasValue)
                {
                    var msg = Encoding.UTF8.GetString(buffer);
                    _logger.LogInformation("received msg:{0}", msg);
                    var response = Encoding.UTF8.GetBytes("ok");
                    await webSocketInfo.Client.SendAsync(new ArraySegment<byte>(response, 0, response.Length),
                        WebSocketMessageType.Text, true, CancellationToken.None);

                }
            } while (!result.CloseStatus.HasValue);
            _logger.LogInformation("client closed, closeStatus:{0} closeDesc:{1}", result.CloseStatus, result.CloseStatusDescription);
            await _connectionManager.RemoveClient(webSocketInfo, result.CloseStatus, result.CloseStatusDescription);
        }
    }
}