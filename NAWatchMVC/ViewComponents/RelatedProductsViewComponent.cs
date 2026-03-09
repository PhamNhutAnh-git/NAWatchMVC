using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.Data;
using NAWatchMVC.ViewModels;
using Microsoft.EntityFrameworkCore; // Phải có dòng này mới dùng được ToListAsync
namespace NAWatchMVC.ViewComponents
{
    public class RelatedProductsViewComponent : ViewComponent
    {
        private readonly NawatchMvcContext _db;
        public RelatedProductsViewComponent(NawatchMvcContext context) => _db = context;

        public async Task<IViewComponentResult> InvokeAsync(int maLoai, int maHhHienTai)
        {
            var products = await _db.HangHoas
                .Where(p => p.MaLoai == maLoai && p.MaHh != maHhHienTai)
                .Take(4)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHh = p.TenHh,
                    Hinh = p.Hinh,
                    DonGia = p.DonGia ?? 0,
                    GiamGia = p.GiamGia ?? 0
                }).ToListAsync();

            return View(products);
        }
    }
}
