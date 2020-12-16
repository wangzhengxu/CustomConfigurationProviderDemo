using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Demo.Core.model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly EmailConfiguration _emailConfiguration;
        private ClientWebSocket WebsocketClient { get; set; }
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IOptions<EmailConfiguration> emailConfiguration)
        {
            _logger = logger;
            _configuration = configuration;
            _emailConfiguration = emailConfiguration.Value;
        }

        public IActionResult Index()
        {
            //            var host = _configuration["Email:Host"];
            return View();
        }

        public async Task<IActionResult> SocketTest()
        {
            var conn = await TryConnectServerAsync();
            if (conn)
            {
                HandleReceivingMessageAsync();

            }
            return Ok("");
        }

        public IActionResult Privacy()
        {

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<bool> TryConnectServerAsync()
        {
            if (WebsocketClient == null)
            {
                WebsocketClient = new ClientWebSocket();
            }
            if (WebsocketClient.State == WebSocketState.Open)
            {
                return true;
            }
            WebsocketClient.Options.SetRequestHeader("appid", "App01_YWJhYjEyMyM=");
            try
            {
                await WebsocketClient.ConnectAsync(new Uri("ws://localhost:5023/ws"), CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return false;
            }

            return true;


        }

        private void SendMsgAsync()
        {
            Task.Run(async () =>
            {
                var data = Encoding.UTF8.GetBytes("ping");
                var i = 0;
                while (i < 10)
                {
                    await Task.Delay(1000);
                    if (WebsocketClient?.State==WebSocketState.Open)
                    {
                        try
                        {
                            await WebsocketClient.SendAsync(new ArraySegment<byte>(data, 0, data.Length),
                                WebSocketMessageType.Text, true,
                                CancellationToken.None);
                            _logger.LogInformation("sent ping to server");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e.ToString());
                        }
                    }

                    i++;
                }
            });
        }

        private void HandleReceivingMessageAsync()
        {
            Task.Run(async () =>
            {
                while (WebsocketClient?.State == WebSocketState.Open)
                {
                    var buffer = new ArraySegment<byte>(new byte[1024 * 1]);
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await WebsocketClient.ReceiveAsync(buffer, CancellationToken.None);

                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.ToString());
                        throw;
                    }
                    if (result?.CloseStatus != null)
                    {
                        _logger.LogInformation("websocket closed");
                        break;
                    }
                    var msg= Encoding.UTF8.GetString(buffer);

                    _logger.LogInformation("received msg:{0}", msg);
                }
            });
        }
    }
}
