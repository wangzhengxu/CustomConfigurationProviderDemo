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

        static async Task Main(string[] args)
        {

            Console.CancelKeyPress += async delegate (object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                if (WebsocketClient.State == WebSocketState.Open)
                {
                    await WebsocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "client closed", CancellationToken.None);
                }

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
                var data = Encoding.UTF8.GetBytes("ping");
                var i = 0;
                while (i < 10)
                {
                    await Task.Delay(1000);
                    if (WebsocketClient?.State == WebSocketState.Open)
                    {
                        try
                        {
                            await WebsocketClient.SendAsync(new ArraySegment<byte>(data, 0, data.Length),
                                WebSocketMessageType.Text, true,
                                CancellationToken.None);
                            Console.WriteLine("sent ping to server");
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
                while (WebsocketClient?.State == WebSocketState.Open)
                {
                    var buffer = new ArraySegment<byte>(new byte[1024 * 1]);
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await WebsocketClient.ReceiveAsync(buffer, CancellationToken.None);
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        break;
                    }
                    if (result?.CloseStatus != null)
                    {
                        Console.WriteLine("websocket closed");
                        break;
                    }
                    var msg = Encoding.UTF8.GetString(buffer);

                    Console.WriteLine("received msg:{0}", msg);
                }
            });
        }
    }
}
