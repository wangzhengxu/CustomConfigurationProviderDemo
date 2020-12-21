using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace ConfigurationCenterApi
{
    public sealed class ConnectionManager
    {
        private static readonly Lazy<ConnectionManager> Lazy = new Lazy<ConnectionManager>(() => new ConnectionManager());
        public static ConnectionManager Instance => Lazy.Value;
        private ConnectionManager()
        {
        }
   

        public static readonly ConcurrentDictionary<int, WebsocketClientInfo> Clients = new ConcurrentDictionary<int, WebsocketClientInfo>();
        private readonly ILogger _logger = Log.ForContext<ConnectionManager>();

        public void AddClient(int socketId, WebsocketClientInfo info)
        {
            
            Clients.TryAdd(socketId, info);
        }

        public void RemoveClient(WebsocketClientInfo info)
        {
            Clients.TryRemove(info.SocketId, out _);
        }
        public bool RemoveClient(int socketId)
        {
            return Clients.TryRemove(socketId, out _);
        }



        public async Task SendToAppClient(string appId, string message)
        {
            var msgBuf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            var result = Clients.Where(x => x.Value.AppId == appId);
            var keyValuePairs = result as KeyValuePair<int, WebsocketClientInfo>[] ?? result.ToArray();
            if (keyValuePairs.Any())
            {
                foreach (var item in keyValuePairs)
                {
                    var client = item.Value.Client;
                    if (client.State == WebSocketState.Open)
                    {
                        await client.SendAsync(msgBuf, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
        }

        public  async Task CloseAllSocketsAsync(Action otherOperations)
        {
            var disposeList = new List<WebSocket>(Clients.Count);
            while (Clients.Count > 0)
            {
                var client = Clients.ElementAt(0).Value;
                if (client.Client.State != WebSocketState.Open)
                {
                    continue;
                }

                _logger.Information($"Closing socket...{client.SocketId}");
                var timeout = new CancellationTokenSource(5 * 1000);
                try
                {
                    await client.Client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token);
                }
                catch (Exception e)
                {
                    _logger.Error(e.ToString());
                }

                if (Clients.TryRemove(client.SocketId, out _))
                {
                    disposeList.Add(client.Client);
                }
                _logger.Information("Done...");
            }

            otherOperations();
            foreach (var socket in disposeList)
            {
                socket.Dispose();
            }
        }
    }
}