using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Demo.Core.model;

namespace WebApp.Infrastructure
{
    public class ConfigurationWorkshop
    {

        public ConcurrentDictionary<string, string> Data { get; set; } = new ConcurrentDictionary<string, string>();

        private readonly MyConfigurationOptions _configurationOptions;



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

            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get,
                _configurationOptions.Url);
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var options=new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                await using var responseStream = await response.Content.ReadAsStreamAsync();
                var configs = await JsonSerializer.DeserializeAsync
                    <IEnumerable<ConfigItem>>(responseStream, options);
                foreach (var configItem in configs)
                {
                    var key = GenerateKey(configItem);
                    var value = configItem.Value;
                    Data.TryAdd(key, value);
                }
            }
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