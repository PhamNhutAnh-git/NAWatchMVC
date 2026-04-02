using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.Data;
using NAWatchMVC.ViewModels;
using NAWatchMVC.Helpers; // Thay 'NAWatchMVC' bằng tên Project của bạn nếu khác
namespace NAWatchMVC.ViewComponents
{
    public class RecentlyViewedViewComponent : ViewComponent
    {
        private readonly NawatchMvcContext _db;
        public RecentlyViewedViewComponent(NawatchMvcContext db) => _db = db;

        public IViewComponentResult Invoke(int limit = 6, bool isHome = false)
        {
            var viewedIds = HttpContext.Session.Get<List<int>>("ViewedProducts") ?? new List<int>();

            // Lấy thông tin sản phẩm từ DB dựa trên list ID trong session
            var products = _db.HangHoas
                .Where(p => viewedIds.Contains(p.MaHh))
                .ToList()
                .OrderBy(p => viewedIds.IndexOf(p.MaHh)) // Sắp xếp đúng thứ tự đã xem
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHh = p.TenHh,
                    Hinh = p.Hinh,
                    DonGia = p.DonGia ?? 0
                }).ToList();
            // Nếu truyền isHome = true thì dùng file Home.cshtml, ngược lại dùng Default.cshtml
            if (isHome) return View("Home", products);
            return View(products);
        }
    }
}
