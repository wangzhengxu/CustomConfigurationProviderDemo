using System;
using Microsoft.Extensions.Configuration;

namespace WebApp.Infrastructure
{
    public static class MyConfigurationExtensions
    {
        public static IConfigurationBuilder AddMyConfiguration(this IConfigurationBuilder configuration,
            Action<MyConfigurationOptions> options)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            var myConfigurationOptions = new MyConfigurationOptions();
            options(myConfigurationOptions);
            configuration.Add(new MyConfigurationSource(myConfigurationOptions));
            return configuration;
        }
    }
}