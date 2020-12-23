using System.Linq;
using Microsoft.Extensions.Configuration;

namespace WebApp.Infrastructure
{
    public class MyConfigurationProvider : ConfigurationProvider
    {
        private readonly MyConfigurationOptions _myConfigurationOptions;

        public MyConfigurationProvider(MyConfigurationOptions myConfigurationOptions)
        {
            _myConfigurationOptions = myConfigurationOptions;
        }

        public override void Load()
        {
            var workshop=new ConfigurationWorkshop(_myConfigurationOptions);
            workshop.ConfigurationChanged += this.OnReload;
            workshop.Connect().GetAwaiter().GetResult();
            Data = workshop.Data;
        }
    }
}