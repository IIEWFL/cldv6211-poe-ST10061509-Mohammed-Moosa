using Microsoft.AspNetCore.Mvc;

namespace EventEase.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

