using System.Net.WebSockets;

namespace ConfigurationCenterApi
{
    public class WebsocketClientInfo
    {
        public string Id { get; set; }
        public string AppId { get; set; }
        public WebSocket Client { get; set; }
    }
}