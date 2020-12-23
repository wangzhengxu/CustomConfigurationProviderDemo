using Microsoft.Extensions.Configuration;

namespace WebApp.Infrastructure
{
    public class MyConfigurationSource : IConfigurationSource
    {
        private readonly MyConfigurationOptions _myConfigurationOptions;


        public MyConfigurationSource(MyConfigurationOptions myConfigurationOptions)
        {
            _myConfigurationOptions = myConfigurationOptions;

        }
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new MyConfigurationProvider(_myConfigurationOptions);
        }
    }

    public class MyConfigurationOptions
    {
        public string Url { get; set; }
        public string AppId { get; set; }
        public string ServerWebsocketUrl { get; set; }
    }
}