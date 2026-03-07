using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.ViewModels;
using NAWatchMVC.Data; // QUAN TRỌNG: Thêm dòng này vì Context nằm trong folder Data

namespace NAWatchMVC.ViewComponents
{
    public class MenuLoaiViewComponent : ViewComponent
    {
        // Đổi tên thành NawatchMvcContext cho đúng với file trong hình của bạn
        private readonly NawatchMvcContext db;

        public MenuLoaiViewComponent(NawatchMvcContext context) => db = context;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Kiểm tra: Nếu db.Loai bị gạch đỏ, hãy thử đổi thành db.Loais (thêm s)
            var data = db.Loais.Select(lo => new MenuLoaiVM
            {
                MaLoai = lo.MaLoai,
                TenLoai = lo.TenLoai,
                // LƯU Ý: Nếu HangHoa bị đỏ, hãy đổi thành HangHoas (thường EF sẽ tự thêm s)
                SoLuong = lo.HangHoas.Count()
            }).OrderBy(p => p.TenLoai);

            return View(data);// send defaut
        }
    }
}