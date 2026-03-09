using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;
using NAWatchMVC.ViewModels;

namespace NAWatchMVC.ViewComponents
{
    public class FeaturedProductsViewComponent : ViewComponent
    {
        private readonly NawatchMvcContext _db;

        // Sửa lại Constructor để gán dữ liệu đúng cách
        public FeaturedProductsViewComponent(NawatchMvcContext context)
        {
            _db = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int limit = 5)
        {
            // Sử dụng ToListAsync() để đồng bộ với await
            var products = await _db.HangHoas
                .OrderByDescending(p => p.SoLanXem)
                .Take(limit)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHh = p.TenHh,
                    Hinh = p.Hinh,
                    // Nếu vẫn lỗi gạch đỏ ở đây, hãy kiểm tra lại file Models/HangHoa.cs nhé!
                    DonGia = p.DonGia??0,
                    GiamGia = p.GiamGia ?? 0 // Thêm ?? 0 nếu GiamGia trong DB cho phép Null
                }).ToListAsync();

            return View(products);
        }
    }
}