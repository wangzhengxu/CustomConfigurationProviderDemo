using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigurationCenterApi
{
    public class ConnectionManager
    {
        private ConcurrentBag<WebsocketClientInfo> _clientList = new ConcurrentBag<WebsocketClientInfo>();

        public void AddClient(WebsocketClientInfo info)
        {
            _clientList.Add(info);
        }

        public async Task RemoveClient(WebsocketClientInfo info, WebSocketCloseStatus? closeStatus, string closeDesc = "")
        {
            if (_clientList.TryTake(out _))
            {
                if (info.Client.State == WebSocketState.Open)
                {
                    await info.Client.CloseAsync(closeStatus ?? WebSocketCloseStatus.Empty, closeDesc, CancellationToken.None);
                    info.Client.Dispose();
                }
            }

        }
    }
}