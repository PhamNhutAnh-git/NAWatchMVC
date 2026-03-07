using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.Data;
using NAWatchMVC.ViewModels;

namespace NAWatchMVC.ViewComponents
{
    public class MenuNCCViewComponent : ViewComponent
    {
        private readonly NawatchMvcContext db;
        public MenuNCCViewComponent(NawatchMvcContext context) => db = context;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var data = db.NhaCungCaps.Select(ncc => new MenuNCCVM
            {
                MaNcc = ncc.MaNcc,
                TenNcc = ncc.MaNcc,
                // Đếm số lượng đồng hồ thuộc nhà cung cấp này
                SoLuong = ncc.HangHoas.Count()
            }).OrderBy(p => p.TenNcc);

            return View(data);
        }
    }
}