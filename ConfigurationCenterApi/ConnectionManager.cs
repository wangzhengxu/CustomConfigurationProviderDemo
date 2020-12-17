using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigurationCenterApi
{
    public sealed class ConnectionManager
    {
        private static readonly Lazy<ConnectionManager> Lazy = new Lazy<ConnectionManager>(() => new ConnectionManager());
        public static ConnectionManager Instance => Lazy.Value;
        private object _lock = new object();
        private ConnectionManager()
        {
        }

        private readonly List<WebsocketClientInfo> _clientList = new List<WebsocketClientInfo>();

        public void AddClient(WebsocketClientInfo info)
        {
            lock (_lock)
            {
                _clientList.Add(info);
            }

        }

        public async Task RemoveClient(WebsocketClientInfo info, WebSocketCloseStatus? closeStatus, string closeDesc = "")
        {
            lock (_lock)
            {
                if (_clientList.Contains(info))
                {
                    _clientList.Remove(info);

                }
            }
            if (info.Client.State == WebSocketState.Open||(info.Client.State!=WebSocketState.Open&&info.Client.State!=WebSocketState.Connecting))
            {
                await info.Client.CloseAsync(closeStatus ?? WebSocketCloseStatus.Empty, closeDesc, CancellationToken.None);
                info.Client.Dispose();
            }


        }
    }
}