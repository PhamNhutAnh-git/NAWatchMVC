using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.Data;
using NAWatchMVC.Helpers;
using NAWatchMVC.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace NAWatchMVC.ViewComponents
{
    public class CartViewComponent : ViewComponent
    {
        private readonly NawatchMvcContext db;
        public CartViewComponent(NawatchMvcContext context) => db = context;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int totalItems = 0;
            // Logic Hybrid: 
            // 1. Nếu đã đăng nhập: Lấy số lượng từ Database (bảng ChiTietGioHang)
            // 2. Nếu chưa đăng nhập: Lấy số lượng từ Session (MYCART)
            if (User.Identity.IsAuthenticated)
            {
                // 1. Lấy CustomerId từ Claims
                var maKh = HttpContext.User.FindFirst("CustomerId")?.Value;

                // 2. Đi thẳng từ bảng GioHang để đếm (Cách này nhanh và sạch nhất)
                var gioHang = await db.GioHangs
                    .Include(gh => gh.ChiTietGioHangs)
                    .FirstOrDefaultAsync(gh => gh.MaKh == maKh);

                if (gioHang != null)
                {
                    // Tính tổng SoLuong của các item trong giỏ
                    totalItems = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuong??0);
                }
            }
            else
            {
                // Khách vãng lai: Đếm trong Session
                var cart = HttpContext.Session.Get<List<CartItemVM>>("MYCART") ?? new List<CartItemVM>();
                totalItems = cart.Sum(c => c.SoLuong);
            }

            return View(totalItems); // Trả về con số lượng
        }
    }
}