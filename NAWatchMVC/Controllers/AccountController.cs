using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace NAWatchMVC.Controllers // Ní nhớ kiểm tra lại Namespace cho đúng dự án của mình
{
    public class AccountController : Controller
    {
        // Hàm Đăng xuất (Logout)
        public async Task<IActionResult> Logout()
        {
            // 1. Xóa sạch "vé" đăng nhập (Cookie Authentication)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Xóa sạch Session (nếu ní có dùng Session để lưu thông tin tạm)
            HttpContext.Session.Clear();

            // 3. Đuổi người dùng về trang chủ của Website (phần khách hàng)
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}