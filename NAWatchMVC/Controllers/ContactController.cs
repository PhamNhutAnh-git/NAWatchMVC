using Microsoft.AspNetCore.Mvc;

namespace NAWatchMVC.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Message"] = "Chào ní! NAWatch luôn sẵn sàng lắng nghe.";
            return View();
        }
    }
}
