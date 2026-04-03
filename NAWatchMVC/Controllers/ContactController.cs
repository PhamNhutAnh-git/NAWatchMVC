using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;
using NAWatchMVC.ViewModels;

namespace NAWatchMVC.Controllers
{
    public class ContactController : Controller
    {
        private readonly NawatchMvcContext _db;
            public ContactController(NawatchMvcContext context) => _db = context;

            [HttpGet]
            public IActionResult Index()
            {
                // Truyền Hotline ra giao diện như ní đang dùng trong View
                ViewBag.Hotline = "0373418869";
                return View();
            }

            [HttpPost]
            public async Task<IActionResult> GuiGopY(GopYVM model)
            {
                if (ModelState.IsValid)
                {
                    var gopy = new GopY
                    {
                        MaGy = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        HoTen = model.HoTen,
                        Email = model.Email,
                        NoiDung = model.NoiDung,
                        NgayGy = DateTime.Now,
                        IsRead = false,
                        IsVisible = true
                    };
                    _db.Add(gopy);
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, message = "Gửi yêu cầu thành công! NAWatch sẽ liên hệ ní sớm nhất. 🚀" });
                }
                return Json(new { success = false, message = "Ní kiểm tra lại thông tin nhập nhé!" });
            }    
        
    }
}
