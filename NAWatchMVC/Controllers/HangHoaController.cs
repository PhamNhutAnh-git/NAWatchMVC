using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;
using NAWatchMVC.ViewModels; // QUAN TRỌNG: Thêm namespace này
using System.Linq;
using NAWatchMVC.Helpers; // Thay 'NAWatchMVC' bằng tên Project của bạn nếu khác
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.AspNetCore.Authorization;
using NAWatchMVC.Services.Implementations;
using NAWatchMVC.Services.Interfaces;
using NAWatchMVC.Services.Interfaces;
namespace NAWatchMVC.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly NawatchMvcContext db;
        private readonly IInteractionService _interactionService; // Thêm dòng này
        public HangHoaController(NawatchMvcContext context, IInteractionService interactionService)
        {
            db = context;
            _interactionService = interactionService; // Gán vào đây
        }

        public async Task<IActionResult> Index(int? page, string mucGia, string query,int? loai, string ncc, double? giaMin, double? giaMax, string loaiMay, string day, string sort)
        {
            // 0,5. Cấu hình phân trang
            int pageSize = 16; // Số sản phẩm mỗi trang
            int pageNumber = (page ?? 1); // Trang hiện tại, mặc định là 1
            // 1. Khởi tạo truy vấn
            var hangHoas = db.HangHoas.AsQueryable();

            // --- ĐOẠN CODE TÍCH HỢP WISHLIST ---
            // 1. Lấy MaKh từ Claim (Đảm bảo đồng bộ với hệ thống Identity ní đang dùng)
            var maKh = User.Claims.FirstOrDefault(c => c.Type == "CustomerId")?.Value;

            // 2. Nếu khách đã đăng nhập, đi lấy danh sách mã sản phẩm họ đã thích
            if (!string.IsNullOrEmpty(maKh))
            {
                ViewBag.LikedProductIds = await db.YeuThiches
                    .Where(y => y.MaKh == maKh)
                    .Select(y => y.MaHh)
                    .ToListAsync();
            }
            else
            {
                ViewBag.LikedProductIds = new List<int>(); // Nếu chưa đăng nhập thì gửi list rỗng
            }
            // ----------------------------------

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
                        hangHoas = hangHoas.Where(p => (p.DonGia * (1 - (p.GiamGia ?? 0) / 100.0)) <= 2000000);
                        break;
                    case "2tr-5tr":
                        hangHoas = hangHoas.Where(p =>
                            (p.DonGia * (1 - (p.GiamGia ?? 0) / 100.0)) >= 2000000 &&
                            (p.DonGia * (1 - (p.GiamGia ?? 0) / 100.0)) <= 5000000);
                        break;
                    case "tren-10tr":
                        hangHoas = hangHoas.Where(p => (p.DonGia * (1 - (p.GiamGia ?? 0) / 100.0)) >= 10000000);
                        break;
                }
            }
            // 3. Lọc theo Tìm kiếm Nâng cao (Keyword Splitting)
            Console.WriteLine("===> Dữ liệu khách tìm là: " + query);
            if (!string.IsNullOrEmpty(query))
            {
                // Tách chuỗi "đồng hồ nam dây da" thành ["đồng", "hồ", "nam", "dây", "da"]
                var keywords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in keywords)
                {
                    var k = word.Trim();
                    // Với mỗi từ, sản phẩm phải thỏa mãn ít nhất 1 cột (Tìm kiếm tịnh tiến)
                    hangHoas = hangHoas.Where(p =>
                        p.TenHh.ToLower().Contains(k) ||
                        (p.ChatLieuDay != null && p.ChatLieuDay.ToLower().Contains(k)) ||
                        (p.MoTa != null && p.MoTa.ToLower().Contains(k)) ||
                        (p.LoaiMay != null && p.LoaiMay.ToLower().Contains(k)) ||
                        (p.GioiTinh != null && p.GioiTinh.ToLower().Contains(k)) || // Tìm chữ "Nam/Nữ" ở đây
                        (p.MaNcc != null && p.MaNcc.ToLower().Contains(k))
                    );
                }
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
                case "price_asc": hangHoas = hangHoas.OrderBy(p => p.DonGia * (1 - (p.GiamGia ?? 0) / 100.0)); break;
                case "price_desc": hangHoas = hangHoas.OrderByDescending(p => p.DonGia * (1 - (p.GiamGia ?? 0) / 100.0)); break;
                default: hangHoas = hangHoas.OrderByDescending(p => p.MaHh); break;
            }
            
            // 6. CHUYỂN ĐỔI SANG HANGHOAVM (Mapping) Đây là bước quan trọng nhất để truyền dữ liệu vào _ProductItem
            var mappingResult = hangHoas.Select(p => new HangHoaVM
            {
                MaHh = p.MaHh,
                TenHh = p.TenHh,
                Hinh = p.Hinh ?? "default.jpg",
                DonGia = p.DonGia ?? 0,
                SoLuong = p.SoLuong ?? 0, // THÊM DÒNG NÀY NÈ NÍ!
                // Ép kiểu double? về int an toàn
                GiamGia = (double)(p.GiamGia ?? 0),

                // Lấy dữ liệu thật từ các cột trong bảng của bạn
                DiemDanhGia =(int) (p.DiemDanhGia ?? 5), // Nếu DB null thì mặc định 5 sao
                SoLuongBan = p.SoLuongBan ?? 10,

                // Ánh xạ các thông số kỹ thuật
                LoaiMay = p.NguonNangLuong ?? "Đang cập nhật",
                DoRongDay = p.DoRongDay ?? "18 mm", // Giả sử cột là DoMongDay
                ChatLieuKinh = p.ChatLieuKinh ?? " kính cường lực", // Có thể lấy từ p.ChatLieu nếu có cột

                NhanUuDai = (p.GiamGia ?? 0) > 10 ? "Giảm sốc" : "Trả chậm 0%",
                // tim
                //IsLiked = likedIds.Contains(p.MaHh)
            });
            // --- BẮT ĐẦU PHÂN TRANG TẠI ĐÂY ---

            // a. Đếm tổng số lượng sau khi đã LỌC sạch sẽ
            int totalItems = await hangHoas.CountAsync();
            // THÊM DÒNG NÀY để đếm số kỹ hơn
            ViewBag.TotalItems = totalItems;
            // b. Tính tổng số trang
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = pageNumber;

            // c. Gửi lại các bộ lọc vào ViewBag để các nút "Trang 2, 3..." không bị mất lọc
            ViewBag.CurrentQuery = query;
            ViewBag.CurrentSort = sort;
            ViewBag.CurrentLoai = loai;
            ViewBag.CurrentMucGia = mucGia;
            ViewBag.CurrentNCC = ncc;

            // d. Cắt dữ liệu (Phải Skip/Take TRƯỚC khi ToList)
            var pagedData = mappingResult
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
            // 3. CHỈ CÓ 1 LỆNH RETURN DUY NHẤT VỚI BIẾN pagedData
            return View(await pagedData.ToListAsync());
        }

        public async Task<IActionResult> Detail(int id)
        {
            // 1. Tìm sản phẩm theo ID và lấy kèm thông tin Loại/NCC
            var hangHoa = await db.HangHoas
                .Include(p => p.MaLoaiNavigation)
                .Include(h => h.DanhGia) // <--- ĐIỀN ĐÚNG "DANHGIA" VÀO ĐÂY
                .Include(p => p.MaNccNavigation)
                .SingleOrDefaultAsync(p => p.MaHh == id);
                
            if (hangHoa == null)
            {
                return RedirectToAction("Index");
            }
            //A.Tìm sản phẩm và tăng lượt xem
            hangHoa.SoLanXem++;
            db.Update(hangHoa);
            await db.SaveChangesAsync();
            // B. === CHÈN LOGIC GỢI Ý MUA SẮM Ở ĐÂY ===
            var maKh = User.Claims.FirstOrDefault(c => c.Type == "CustomerId")?.Value;
            if (!string.IsNullOrEmpty(maKh))
            {
                // Ghi lại tương tác vào bảng UserInteraction (Type 1 = Xem)
                // Dùng await vì hàm Record là Task
                await _interactionService.Record(maKh, id, 1);
            }
            // =========================================
            // sử lý sp tương tự
            // 2. Lấy danh sách sản phẩm tương tự (Cùng MaLoai nhưng khác MaHh hiện tại)
            var dsTuongTu = await db.HangHoas
                .Where(p => p.MaLoai == hangHoa.MaLoai && p.MaHh != id)
                .Take(4) // Lấy 4 sản phẩm để hiện thành 1 hàng
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHh = p.TenHh,
                    Hinh = p.Hinh,
                    DonGia = p.DonGia ?? 0,
                    GiamGia = p.GiamGia ?? 0,
                    TenLoai = p.MaLoaiNavigation.TenLoai
                }).ToListAsync();
            // 2. Mapping toàn bộ 27 thuộc tính sang ViewModel
            var model = new ChiTietHangHoaVM
            {
                MaHh = hangHoa.MaHh,
                TenHh = hangHoa.TenHh,
                TenAlias = hangHoa.TenAlias??"",
                MaLoai = hangHoa.MaLoai,
                MaNcc = hangHoa.MaNcc,
                DonGia = hangHoa.DonGia ?? 0,
                GiamGia = (double)(hangHoa.GiamGia ?? 0),
                Hinh = hangHoa.Hinh ?? "default.jpg",
                NgaySx = hangHoa.NgaySx ?? DateTime.Now,
                SoLanXem = hangHoa.SoLanXem ?? 0,
                MoTa = hangHoa.MoTa ?? "Đang cập nhật",
                SoLuongBan = hangHoa.SoLuongBan ?? 0,
                SoLuong = hangHoa.SoLuong ?? 0,
                GioiTinh = hangHoa.GioiTinh ?? "Unisex",
                DuongKinhMat = hangHoa.DuongKinhMat ?? "Đang cập nhật",
                ChatLieuDay = hangHoa.ChatLieuDay ?? "Đang cập nhật",
                DoRongDay = hangHoa.DoRongDay ?? "Đang cập nhật",
                ChatLieuKhungVien = hangHoa.ChatLieuKhungVien ?? "Đang cập nhật",
                ChatLieuKinh = hangHoa.ChatLieuKinh ?? "Kính khoáng",
                TenBoMay = hangHoa.TenBoMay ?? "Đang cập nhật",
                ChongNuoc = hangHoa.ChongNuoc ?? "3 ATM",
                TienIch = hangHoa.TienIch ?? "Không có",
                NguonNangLuong = hangHoa.NguonNangLuong ?? "Pin (Quartz)",
                LoaiMay = hangHoa.LoaiMay ?? "Đang cập nhật",
                BoSuuTap = hangHoa.BoSuuTap ?? "Cơ bản",
                XuatXu = hangHoa.XuatXu ?? "Chính hãng",
                DiemDanhGia = (int)(hangHoa.DiemDanhGia ?? 5),
                ThoiGianPin = hangHoa.ThoiGianPin ?? 0, // Nếu null thì để là 0
                SanPhamTuongTu = dsTuongTu,
                // Tên hiển thị từ bảng liên kết
                TenLoai = hangHoa.MaLoaiNavigation?.TenLoai??"chưa phân loại",
                TenNcc = hangHoa.MaNccNavigation?.MaNcc??"",
                DanhGias = hangHoa.DanhGia
                .Where(dg => dg.TrangThai == true) // Chỉ lấy đánh giá đã duyệt
                .ToList()
                //DanhGias = hangHoa.DanhGia.ToList()
            };
            // --- LOGIC LƯU SẢN PHẨM ĐÃ XEM ---
            var viewedIds = HttpContext.Session.Get<List<int>>("ViewedProducts") ?? new List<int>();

            // Nếu sản phẩm chưa có trong danh sách thì mới thêm vào
            if (!viewedIds.Contains(id))
            {
                viewedIds.Insert(0, id); // Thêm vào đầu danh sách
                if (viewedIds.Count > 10) viewedIds.RemoveAt(10); // Chỉ giữ tối đa 10 sản phẩm
                HttpContext.Session.Set("ViewedProducts", viewedIds);
            }

            return View(model);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GuiDanhGia(int MaHh, int Sao, string NoiDung)
        {
            // 1. KIỂM TRA QUYỀN: Nếu là Admin thì chặn lại ngay
            if (User.IsInRole("Admin"))
            {
                // Gửi thông báo đỏ rực để Admin biết mình bị chặn
                TempData["Error"] = "Ní đang là Admin, chỉ được xem thôi chứ không được 'tự luyến' đánh giá đâu nha!";

                // Trả về đúng cái view chi tiết sản phẩm đó
                return RedirectToAction("Detail", new { id = MaHh });
            }
            var maKh = User.FindFirst("CustomerId")?.Value;
            // KIỂM TRA XEM ĐÃ MUA HÀNG CHƯA
            bool daMua = await db.HoaDons
                .AnyAsync(hd => hd.MaKh == maKh &&
                                hd.MaTrangThai == 3 && // Giả sử 3 là Đã giao hàng thành công
                                db.ChiTietHds.Any(ct => ct.MaHd == hd.MaHd && ct.MaHh == MaHh));

            if (!daMua)
            {
                // Dùng cái tên khác để phân biệt
                TempData["Error"] = "Ní chưa mua hàng mà, mua đi rồi mới cho đánh giá nha! ❌";
                return RedirectToAction("Detail", new { id = MaHh });
            }
            // 1. Lưu đánh giá mới (dùng db.DanhGium vì tên DbSet của ní là DanhGium)
            var dg = new DanhGium
            {
                MaHh = MaHh,
                MaKh = maKh,
                Sao = Sao,
                NoiDung = NoiDung,
                NgayDang = DateTime.Now,
                TrangThai = true
            };
            db.DanhGia.Add(dg);
            await db.SaveChangesAsync();

            // 2. TÍNH LẠI TRUNG BÌNH CỘNG
            // Lấy tất cả số sao của sản phẩm này trong DB
            var listSao = await db.DanhGia
                .Where(d => d.MaHh == MaHh)
                .Select(d => d.Sao ?? 0)
                .ToListAsync();

            if (listSao.Any())
            {
                double trungBinh = listSao.Average(); // Đây là hàm thần thánh tính trung bình cộng

                // Tìm ông hàng hóa đó để cập nhật điểm mới
                var hh = await db.HangHoas.FindAsync(MaHh);
                if (hh != null)
                {
                    // Làm tròn ví dụ 4.6 thành 5, 4.4 thành 4
                    hh.DiemDanhGia = (int)Math.Round(trungBinh);

                    db.Update(hh);
                    await db.SaveChangesAsync();
                }
            }
            // Nếu thành công thì dùng cái này
            TempData["Success"] = "Đánh giá của ní đã lên sóng! ✅";
            return RedirectToAction("Detail", new { id = MaHh });
        }

        // Trang hiển thị so sánh chi tiết: HangHoa/Compare?ids=123,456,789
        public async Task<IActionResult> Compare(string ids)
        {
            if (string.IsNullOrEmpty(ids)) return RedirectToAction("Index");

            // 1. Chuyển chuỗi ID (ví dụ: "1,2,3") thành danh sách SỐ (List<int>)
            var listMaHh = ids.Split(',')
                              .Select(int.Parse) // Ép kiểu từng phần tử sang int
                              .ToList();

            // 2. Bây giờ câu lệnh Where sẽ chạy "mượt như nhung"
            var model = await db.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .Where(h => listMaHh.Contains(h.MaHh)) // Số so sánh với Số -> Chuẩn bài!
                .Take(3)
                            .Select(h => new ChiTietHangHoaVM
                {
                    MaHh = h.MaHh,
                    TenHh = h.TenHh,
                    TenAlias = h.TenAlias ?? "",
                    MaLoai = h.MaLoai,
                    MaNcc = h.MaNcc,
                    DonGia = h.DonGia ?? 0,
                    GiamGia = (double)(h.GiamGia ?? 0),
                    Hinh = h.Hinh ?? "default.jpg",
                    NgaySx = h.NgaySx ?? DateTime.Now,
                    SoLanXem = h.SoLanXem ?? 0,
                    MoTa = h.MoTa ?? "Đang cập nhật",
                    SoLuongBan = h.SoLuongBan ?? 0,
                    SoLuong = h.SoLuong ?? 0,
                    GioiTinh = h.GioiTinh ?? "Unisex",
                    DuongKinhMat = h.DuongKinhMat ?? "Đang cập nhật",
                    ChatLieuDay = h.ChatLieuDay ?? "Đang cập nhật",
                    DoRongDay = h.DoRongDay ?? "Đang cập nhật",
                    ChatLieuKhungVien = h.ChatLieuKhungVien ?? "Đang cập nhật",
                    ChatLieuKinh = h.ChatLieuKinh ?? "Kính khoáng",
                    TenBoMay = h.TenBoMay ?? "Đang cập nhật",
                    ChongNuoc = h.ChongNuoc ?? "3 ATM",
                    TienIch = h.TienIch ?? "Không có",
                    NguonNangLuong = h.NguonNangLuong ?? "Pin (Quartz)",
                    LoaiMay = h.LoaiMay ?? "Đang cập nhật",
                    BoSuuTap = h.BoSuuTap ?? "Cơ bản",
                    XuatXu = h.XuatXu ?? "Chính hãng",
                    DiemDanhGia = (int)(h.DiemDanhGia ?? 5),
                    ThoiGianPin = h.ThoiGianPin ?? 0, // Nếu null thì để là 0
                    // Tên hiển thị từ bảng liên kết
                    TenLoai = h.MaLoaiNavigation.TenLoai ?? "chưa phân loại",
                    TenNcc = h.MaNccNavigation.MaNcc ?? "",
                })
                .ToListAsync();

            return View(model); // Trả về trang Views/HangHoa/Compare.cshtml
        }

        
    }
}