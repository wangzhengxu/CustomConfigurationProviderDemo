using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public HomeController(ILogger<HomeController> logger,IConfiguration configuration,IOptions<EmailConfiguration>emailConfiguration)
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
