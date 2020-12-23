using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Demo.Core.model;

namespace WebApp.Infrastructure
{
    public class ConfigurationWorkshop
    {

        public ConcurrentDictionary<string, string> Data { get; set; } = new ConcurrentDictionary<string, string>();

        private readonly MyConfigurationOptions _configurationOptions;
        private ClientWebSocket WebsocketClient { get; set; }
        public event Action ConfigurationChanged;

        public ConfigurationWorkshop(MyConfigurationOptions myConfiguration)
        {
            _configurationOptions = myConfiguration;
        }

        public async Task Connect()
        {
            if (_configurationOptions == null || string.IsNullOrEmpty(_configurationOptions.Url))
            {
                throw new ArgumentException("configuration options is not valid");
            }

            if (await TryConnectServer())
            {
                ReceiveWebsocketMessage();
            }

            await LoadData();
        }

        private async Task<bool> TryConnectServer()
        {
            if (WebsocketClient == null)
            {
                WebsocketClient = new ClientWebSocket();
            }
            if (WebsocketClient.State == WebSocketState.Open)
            {
                return true;
            }
            WebsocketClient.Options.SetRequestHeader("appid", _configurationOptions.AppId);
            var url = _configurationOptions.ServerWebsocketUrl;
            try
            {
                await WebsocketClient.ConnectAsync(new Uri(url), CancellationToken.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            return true;
        }

        private async Task LoadData()
        {
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get,
                _configurationOptions.Url);
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                await using var responseStream = await response.Content.ReadAsStreamAsync();
                var configs = await JsonSerializer.DeserializeAsync
                    <IEnumerable<ConfigItem>>(responseStream, options);
                Data.Clear();
                foreach (var configItem in configs)
                {
                    var key = GenerateKey(configItem);
                    var value = configItem.Value;
                    Data.TryAdd(key, value);
                }
            }
        }

        private void ReceiveWebsocketMessage()
        {
            Task.Run(async () =>
            {
                try
                {
                    var buffer = WebSocket.CreateClientBuffer(1024 * 4, 1024 * 4);
                    while (WebsocketClient.State != WebSocketState.Closed)
                    {
                        var receiveResult = await WebsocketClient.ReceiveAsync(buffer, CancellationToken.None);
                        if (WebsocketClient.State == WebSocketState.CloseReceived &&
                            receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine("Acknowledging Close frame received from server");
                            await WebsocketClient.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,
                                "Acknowledge Close frame", CancellationToken.None);
                        }

                        if (WebsocketClient.State == WebSocketState.Open &&
                            receiveResult.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(buffer.Array, 0, receiveResult.Count);
                            Console.WriteLine("received msg:{0}", message);
                            if (message == "update")
                            {
                                await LoadData();
                                ConfigurationChanged?.Invoke();
                                Console.WriteLine("configuration updated...");
                            }
                        }

                    }
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

        private string GenerateKey(ConfigItem item)
        {
            var key = new StringBuilder();
            if (!string.IsNullOrEmpty(item.Group))
            {
                key.Append(item.Group + ":");
            }

            key.Append(item.Key);

            return key.ToString();
        }
    }
}