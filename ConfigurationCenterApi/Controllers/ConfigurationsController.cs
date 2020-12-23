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
        private static List<ConfigItem> list= new List<ConfigItem>
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
        [HttpGet]
        public IEnumerable<ConfigItem> Get()
        {
            return list;
        }
        [HttpGet("send_msg")]
        public async Task<IActionResult> SendMsg()
        {
            var appId = "App01_YWJhYjEyMyM=";
            await ConnectionManager.Instance.SendToAppClient(appId, "hello client!");
            return Content("ok");
        }
        [HttpGet("update_conf")]
        public async Task<IActionResult> UpdateConfiguration()
        {

            var appId = "App01_YWJhYjEyMyM=";
            var portInfo = list.FirstOrDefault(x => x.Key == "Port");
            if (portInfo != null)
            {
                portInfo.Value = "25";
            }
            await ConnectionManager.Instance.SendToAppClient(appId, "update");
            return Ok(list);

        }
    }
}
