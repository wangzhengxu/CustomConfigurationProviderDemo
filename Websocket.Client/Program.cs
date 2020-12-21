using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Websocket.Client
{
    class Program
    {
        private static ClientWebSocket WebsocketClient { get; set; }
        private static CancellationTokenSource _socketLoopTokenSource;
        private static CancellationTokenSource _sendLoopTokenSource;

        static async Task Main(string[] args)
        {

            Console.CancelKeyPress += async delegate (object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                Console.WriteLine("Closing connection...");
                _sendLoopTokenSource?.Cancel();
                if (WebsocketClient == null || WebsocketClient.State != WebSocketState.Open) return;
                var timeout = new CancellationTokenSource(1000 * 5);
                try
                {
                    await WebsocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "client closing", timeout.Token);
                    while (WebsocketClient.State != WebSocketState.Closed && !timeout.Token.IsCancellationRequested)
                    {
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                  
                }
                _socketLoopTokenSource?.Cancel();

            };

            var conn = await TryConnectServerAsync();
            if (conn)
            {
                HandleReceivingMessageAsync();
                SendMsgAsync();
            }


            Console.ReadKey();
        }
        private static async Task<bool> TryConnectServerAsync()
        {
            _socketLoopTokenSource = new CancellationTokenSource();
            _sendLoopTokenSource = new CancellationTokenSource();
            if (WebsocketClient == null)
            {
                WebsocketClient = new ClientWebSocket();
            }
            if (WebsocketClient.State == WebSocketState.Open)
            {
                return true;
            }
            WebsocketClient.Options.SetRequestHeader("appid", "App01_YWJhYjEyMyM=");
            try
            {
                await WebsocketClient.ConnectAsync(new Uri("ws://localhost:5023/ws"), CancellationToken.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            return true;


        }

        private static void SendMsgAsync()
        {
            Task.Run(async () =>
            {
                var token = _sendLoopTokenSource.Token;
                var data = Encoding.UTF8.GetBytes("ping");
                var i = 0;
                while (i < 10 && !token.IsCancellationRequested)
                {
                    await Task.Delay(1000, token);
                    if (WebsocketClient?.State == WebSocketState.Open)
                    {
                        try
                        {
                            if (!token.IsCancellationRequested)
                            {
                                await WebsocketClient.SendAsync(new ArraySegment<byte>(data, 0, data.Length),
                                    WebSocketMessageType.Text, true,
                                    CancellationToken.None);
                                Console.WriteLine("sent ping to server");
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }

                    i++;
                }
            });
        }

        private static void HandleReceivingMessageAsync()
        {
            Task.Run(async () =>
            {
                var cancellationToken = _socketLoopTokenSource.Token;
                try
                {
                    var buffer = WebSocket.CreateClientBuffer(1024 * 4, 1024 * 4);
                    while (WebsocketClient.State != WebSocketState.Closed && !cancellationToken.IsCancellationRequested)
                    {
                        var receiveResult = await WebsocketClient.ReceiveAsync(buffer, cancellationToken);
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            if (WebsocketClient.State == WebSocketState.CloseReceived &&
                                receiveResult.MessageType == WebSocketMessageType.Close)
                            {
                                Console.WriteLine("Acknowledging Close frame received from server");
                                _sendLoopTokenSource.Cancel();
                                await WebsocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,
                                    "Acknowledge Close frame", CancellationToken.None);
                            }

                            if (WebsocketClient.State == WebSocketState.Open &&
                                receiveResult.MessageType == WebSocketMessageType.Text)
                            {
                                var message = Encoding.UTF8.GetString(buffer.Array, 0, receiveResult.Count);
                                Console.WriteLine("received msg:{0}", message);
                            }
                        }
                    }

                    Console.WriteLine($"Ending loop in state {WebsocketClient.State}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    WebsocketClient.Dispose();
                }
            });
        }
    }
}
