using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;
using Microsoft.AspNetCore.Authorization;

namespace NAWatchMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")] // Khóa chặt cửa cho Admin
    public class AdminReviewController : Controller
    {
        private readonly NawatchMvcContext _context;

        public AdminReviewController(NawatchMvcContext context)
        {
            _context = context;
        }

        // 1. Trang danh sách đánh giá
        public async Task<IActionResult> Index()
        {
            var dsDanhGia = await _context.DanhGia
                .Include(d => d.MaHhNavigation) // Lấy tên sản phẩm
                .Include(d => d.MaKhNavigation) // Lấy tên khách hàng
                .OrderByDescending(d => d.NgayDang) // Mới nhất lên đầu
                .ToListAsync();

            return View(dsDanhGia);
        }

        // 2. Hàm xử lý Ẩn/Hiện (Toggle)
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var dg = await _context.DanhGia.FindAsync(id);
            if (dg != null)
            {
                // Đảo trạng thái: nếu true thành false, false thành true
                dg.TrangThai = !(dg.TrangThai ?? true);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Đã cập nhật trạng thái đánh giá!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}