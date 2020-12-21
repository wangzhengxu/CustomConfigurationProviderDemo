using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Demo.Core.model;

namespace ConfigurationCenterApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationsController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<ConfigItem> Get()
        {
            return new List<ConfigItem>
            {
                new ConfigItem
                {
                    Key = "Host",
                    Value = "smtp.demo.email",
                    Group = "Email"
                    },
                new ConfigItem
                {
                    Key = "Port",
                    Value = "587",
                    Group = "Email"
                },
                new ConfigItem
                {
                    Key = "UserName",
                    Value = "demo",
                    Group = "Email"
                },
                new ConfigItem
                {
                    Key = "Password",
                    Value = "123123",
                    Group = "Email"
                }
            };
        }
        [HttpGet("send_msg")]
        public async Task<IActionResult> SendMsg()
        {
            var appId = "App01_YWJhYjEyMyM=";
            await ConnectionManager.Instance.SendToAppClient(appId, "hello client!");
            return Content("ok");
        }
    }
}
