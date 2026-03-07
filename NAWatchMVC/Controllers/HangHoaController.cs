using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;
using NAWatchMVC.ViewModels; // QUAN TRỌNG: Thêm namespace này
using System.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace NAWatchMVC.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly NawatchMvcContext db;

        public HangHoaController(NawatchMvcContext context)
        {
            db = context;
        }

        public async Task<IActionResult> Index(string mucGia, string query,int? loai, string ncc, double? giaMin, double? giaMax, string loaiMay, string day, string sort)
        {
            // 1. Khởi tạo truy vấn
            var hangHoas = db.HangHoas.AsQueryable();

            // 2. Các bộ lọc (Nhựt Anh giữ nguyên các đoạn if lọc loai, ncc, gia...)
            if (loai.HasValue) hangHoas = hangHoas.Where(p => p.MaLoai == loai.Value);
            if (!string.IsNullOrEmpty(ncc)) hangHoas = hangHoas.Where(p => p.MaNcc == ncc);
            if (giaMin.HasValue) hangHoas = hangHoas.Where(p => p.DonGia >= giaMin.Value);
            if (giaMax.HasValue) hangHoas = hangHoas.Where(p => p.DonGia <= giaMax.Value);
            if (!string.IsNullOrEmpty(mucGia))
            {
                switch (mucGia)
                {
                    case "duoi-2tr":
                        hangHoas = hangHoas.Where(p => p.DonGia <= 2000000);
                        break;
                    case "2tr-5tr":
                        hangHoas = hangHoas.Where(p => p.DonGia >= 2000000 && p.DonGia <= 5000000);
                        break;
                    case "tren-10tr":
                        hangHoas = hangHoas.Where(p => p.DonGia >= 10000000);
                        break;
                }
            }
            // 3. Lọc theo Tìm kiếm (query) - Thêm phần này để thanh Search hoạt động
            if (!string.IsNullOrEmpty(query))
            {
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(query));
            }
            // 4.Lọc theo Máy & Dây Sửa đoạn lọc: Dùng cột NguonNangLuong thay cho LoaiMay
            if (!string.IsNullOrEmpty(loaiMay))
            {
                // loaiMay là giá trị từ URL (?loaiMay=Pin), NguonNangLuong là tên cột trong DB
                hangHoas = hangHoas.Where(p => p.NguonNangLuong.Contains(loaiMay));
            }
            if (!string.IsNullOrEmpty(day)) hangHoas = hangHoas.Where(p => p.ChatLieuDay.Contains(day));

            // 5. Xử lý Sắp xếp (Sorting) - Phải làm TRƯỚC khi Mapping
            switch (sort)
            {
                case "price_asc": hangHoas = hangHoas.OrderBy(p => p.DonGia); break;
                case "price_desc": hangHoas = hangHoas.OrderByDescending(p => p.DonGia); break;
                default: hangHoas = hangHoas.OrderByDescending(p => p.MaHh); break;
            }
            // 6. CHUYỂN ĐỔI SANG HANGHOAVM (Mapping) Đây là bước quan trọng nhất để truyền dữ liệu vào _ProductItem
            var mappingResult = hangHoas.Select(p => new HangHoaVM
            {
                MaHh = p.MaHh,
                TenHh = p.TenHh,
                Hinh = p.Hinh ?? "default.jpg",
                DonGia = p.DonGia ?? 0,

                // Ép kiểu double? về int an toàn
                GiamGia = (double)(p.GiamGia ?? 0),

                // Lấy dữ liệu thật từ các cột trong bảng của bạn
                DiemDanhGia =(int) (p.DiemDanhGia ?? 5), // Nếu DB null thì mặc định 5 sao
                SoLuongBan = p.SoLuongBan ?? 10,

                // Ánh xạ các thông số kỹ thuật
                LoaiMay = p.NguonNangLuong ?? "Đang cập nhật",
                DoRongDay = p.DoRongDay ?? "18 mm", // Giả sử cột là DoMongDay
                ChatLieuKinh = p.ChatLieuKinh ?? " kính cường lực", // Có thể lấy từ p.ChatLieu nếu có cột

                NhanUuDai = (p.GiamGia ?? 0) > 10 ? "Giảm sốc" : "Trả chậm 0%"
            });
            // QUAN TRỌNG: Truyền danh sách đã lọc (hangHoas) vào View để hết lỗi null
            //var result = await hangHoas.ToListAsync();
            //return View(result);
            return View(await mappingResult.ToListAsync());
        }
    }
}