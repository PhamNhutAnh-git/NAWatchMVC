using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.Data; // Thay bằng namespace Data của ní
using System.Linq;

namespace NAWatchMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")] // <--- CHỈ ADMIN VÀ STAFF MỚI ĐƯỢC VÀO
    public class HomeAdminController : Controller
    {
        private readonly NawatchMvcContext _context;

        public HomeAdminController(NawatchMvcContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. Tính tổng doanh thu (Chỉ tính những đơn đã giao/thanh toán nếu cần)
            ViewBag.TongDoanhThu = _context.HoaDons.Sum(h => (double?)h.TongTien) ?? 0;

            // 2. Đếm số đơn hàng mới (Trạng thái = 0: Chờ xác nhận)
            ViewBag.DonHangMoi = _context.HoaDons.Count(h => h.MaTrangThai == 0);

            // 3. Đếm sản phẩm sắp hết hàng (Số lượng < 5)
            ViewBag.SapHetHang = _context.HangHoas.Count(h => h.SoLuong < 5);

            // 4. Lấy danh sách 5 đơn hàng mới nhất để hiện ở bảng bên dưới
            var donHangMoiNhat = _context.HoaDons
                                        .OrderByDescending(h => h.NgayDat)
                                        .Take(5)
                                        .ToList();

            return View(donHangMoiNhat);
        }
    }
}