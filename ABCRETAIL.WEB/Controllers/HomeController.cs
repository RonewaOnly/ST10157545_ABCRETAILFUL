using Microsoft.AspNetCore.Mvc;
using ABCRETAIL.WEB.Models;
using System.Diagnostics;

namespace ABCRETAIL.WEB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Error()
        {
            return View();
        }

    }
}
