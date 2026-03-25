using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IActionResult> Index()
        {
            // Nới rộng ra 20 ngày để ní thấy dữ liệu cũ (Lúc bảo vệ thì sửa lại 7 nhé)
            var soNgay = 20;
            var ngayBatDau = DateTime.Now.Date.AddDays(-soNgay);

            // Lấy doanh thu thực tế từ DB (Chỉ mã 3)
            var doanhThuDb = _context.HoaDons
                .Where(h => h.MaTrangThai == 3 && h.NgayDat >= ngayBatDau)
                .GroupBy(h => h.NgayDat.Value.Date)
                .Select(g => new { Ngay = g.Key, Tong = g.Sum(h => h.TongTien ?? 0) })
                .ToList();

            // Tạo danh sách 7 ngày liên tiếp để biểu đồ không bị đứt đoạn
            var labels = new List<string>();
            var data = new List<double>();

            for (int i = soNgay; i >= 0; i--)
            {
                var date = DateTime.Now.Date.AddDays(-i);
                labels.Add(date.ToString("dd/MM"));
                var doanhThuNgay = doanhThuDb.FirstOrDefault(x => x.Ngay == date)?.Tong ?? 0;
                data.Add((double)doanhThuNgay);
            }

            ViewBag.Labels7Ngay = labels;
            ViewBag.Data7Ngay = data;

            // --- Fix Top Sản Phẩm (Lấy hình và tên chuẩn) ---
            var topProducts = _context.ChiTietHds
                .Include(ct => ct.MaHhNavigation)
                .Where(ct => ct.MaHdNavigation.MaTrangThai == 3) // Chỉ tính đơn thành công
                .GroupBy(ct => new { ct.MaHh, ct.MaHhNavigation.TenHh, ct.MaHhNavigation.Hinh })
                .Select(g => new {
                    Ten = g.Key.TenHh,
                    Hinh = g.Key.Hinh, // Đảm bảo cột Hinh có đuôi .jpg/png
                    SoLuong = g.Sum(ct => ct.SoLuong)
                })
                .OrderByDescending(x => x.SoLuong).Take(5).ToList();

            ViewBag.TopProducts = topProducts;

            // Các Card (Giữ nguyên logic của ní)
            ViewBag.TongDoanhThu = _context.HoaDons.Where(h => h.MaTrangThai == 3).Sum(h => h.TongTien ?? 0);
            ViewBag.DonHangMoi = _context.HoaDons.Count(h => h.MaTrangThai == 0);
            ViewBag.ShippingCount = _context.HoaDons.Count(h => h.MaTrangThai == 2);
            ViewBag.SapHetHang = _context.HangHoas.Count(h => h.SoLuong < 5);
            // --- THIẾU CÁI NÀY NÈ NÍ - ĐỔ DỮ LIỆU CHO BIỂU ĐỒ TRÒN ---
            ViewBag.PieData = new int[] {
                _context.HoaDons.Count(h => h.MaTrangThai == 3), // Hoàn tất
                _context.HoaDons.Count(h => h.MaTrangThai == 2), // Đang giao
                _context.HoaDons.Count(h => h.MaTrangThai == 4), // Đã hủy
                _context.HoaDons.Count(h => h.MaTrangThai == 5)  // Hoàn tiền
            };
            // Lấy 5-7 hoạt động gần nhất từ bảng HoaDon
            // Mình sẽ giả định: Đơn mới đặt = Hoạt động mới, Đơn vừa đổi trạng thái = Hoạt động cập nhật
            var recentActivities = await _context.HoaDons
                .Include(h => h.MaTrangThaiNavigation)
                .OrderByDescending(h => h.NgayDat) // Hoặc dùng cột NgayCapNhat nếu ní có
                .Select(h => new {
                    MaHd = h.MaHd,
                    KhachHang = h.HoTen,
                    TrangThai = h.MaTrangThaiNavigation.TenTrangThai,
                    ThoiGian = h.NgayDat,
                    MaTrangThai = h.MaTrangThai
                })
                .Take(6)
                .ToListAsync();

            ViewBag.RecentActivities = recentActivities;
            var hoadonGanDay = await _context.HoaDons.Include(h => h.MaTrangThaiNavigation).OrderByDescending(h => h.NgayDat).Take(5).ToListAsync();
            return View(hoadonGanDay);
        }
    }
}