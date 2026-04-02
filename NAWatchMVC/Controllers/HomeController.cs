using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;
using NAWatchMVC.Models;
using NAWatchMVC.ViewModels;
using System.Diagnostics;

namespace NAWatchMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly NawatchMvcContext _context;
        public HomeController(ILogger<HomeController> logger, NawatchMvcContext context)
        {
            _logger = logger;
            _context = context; // Gán giá trị để sử dụng
        }
        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel();

            // 1. Lấy toàn bộ Media - Tối ưu 1 lần gọi DB
            var medias = await _context.HomePageMedias
                                .Where(x => x.IsActive)
                                .OrderBy(x => x.Order)
                                .ToListAsync();
            // 1. Lấy danh sách các hãng ní muốn hiện ở trang chủ (ví dụ lấy 3 hãng đầu tiên)
            var brands = await _context.NhaCungCaps.Take(4).ToListAsync();
            // --- PHÂN LOẠI MEDIA CHO HERO SECTION ---
            ViewBag.TopBackground = medias.FirstOrDefault(x => x.ViTri == "Top" && x.MediaType == "Banner");
            ViewBag.TopSlides = medias.Where(x => x.ViTri == "Top" && x.MediaType == "Slide")
                                      .OrderBy(x => x.Order)
                                      .ToList();

            // Banner giữa trang
            model.Banners = medias.Where(x => x.MediaType == "Banner" && x.ViTri == "Middle").ToList();

            // 2. XỬ LÝ DANH SÁCH VIDEO CHUNG
            // --- PHẦN VIDEO CHUNG (Danh sách Video Review bên dưới) ---
            model.Videos = medias.Where(x => x.MediaType.ToLower() == "video").ToList();
            foreach (var vid in model.Videos)
            {
                var cleanId = GetYouTubeId(vid.YoutubeUrl);
                // Quan trọng: Gán lại nguyên cái link Embed hoàn chỉnh cho danh sách
                vid.YoutubeUrl = $"https://www.youtube.com/embed/{cleanId}";
            }

            // 3. XỬ LÝ VIDEO CHO 4 KHỐI DANH MỤC (Nam, Nữ, Unisex, Trẻ em)
            // --- PHẦN VIDEO BANNER (4 khối Nam, Nữ, Trẻ em, Unisex) ---
            // Chỉ lấy mỗi cái ID sạch để View tự ghép link Embed
            model.VideoIdNam = GetYouTubeId(medias.FirstOrDefault(x => x.ViTri == "VideoNam")?.YoutubeUrl);
            model.VideoIdNu = GetYouTubeId(medias.FirstOrDefault(x => x.ViTri == "VideoNu")?.YoutubeUrl);
            model.VideoIdUnisex = GetYouTubeId(medias.FirstOrDefault(x => x.ViTri == "VideoUnisex")?.YoutubeUrl);
            model.VideoIdTreEm = GetYouTubeId(medias.FirstOrDefault(x => x.ViTri == "VideoTreEm")?.YoutubeUrl);

            // Lấy video đặc biệt cho khối giới thiệu trang web
            model.VideoIdIntro = GetYouTubeId(medias.FirstOrDefault(x => x.ViTri == "VideoIdIntro")?.YoutubeUrl);

            ViewBag.PopupAd = medias.FirstOrDefault(x => x.ViTri == "Popup");

            // 4. DỮ LIỆU BỔ TRỢ (Voucher, Tin tức)
            model.Vouchers = await _context.Vouchers.Where(x => x.TrangThai == true).ToListAsync();
            model.News = await _context.NewsArticles.Where(x => x.IsActive)
                                       .OrderByDescending(x => x.PublishedDate)
                                       .Take(3).ToListAsync();

            // 5. LẤY BỘ SƯU TẬP (Giao diện 6 thẻ/hàng)
            var activeCollections = await _context.BoSuuTapHomes
                                                  .Where(x => x.IsActive)
                                                  .OrderBy(x => x.Order)
                                                  .Take(5).ToListAsync();

            foreach (var bst in activeCollections)
            {
                model.Collections.Add(new CollectionData
                {
                    Name = bst.CollectionName,
                    Image = bst.BackgroundImage,
                    Products = await _context.HangHoas
                        .Where(h => h.BoSuuTap == bst.CollectionName)
                        .Take(6)
                        .Select(h => new HangHoaVM
                        {
                            MaHh = h.MaHh,
                            TenHh = h.TenHh,
                            Hinh = h.Hinh,
                            DonGia = h.DonGia ?? 0,
                            GiamGia = h.GiamGia ?? 0,
                            DiemDanhGia = (int)(h.DiemDanhGia ?? 5),
                            SoLuongBan = h.SoLuongBan ?? 0,
                        }).ToListAsync()
                });
            }

            // 6. LẤY SẢN PHẨM THEO LOẠI
            model.NamProducts = await GetProductsByCategory(1, 4);   // MaLoai = 1
            model.NuProducts = await GetProductsByCategory(2, 4);    // MaLoai = 2
            model.UnisexProducts = await GetProductsByCategory(3, 4); // MaLoai = 3
            model.TreEmProducts = await GetProductsByCategory(4, 4);  // MaLoai = 4
            // 7. lấy các ncc
            foreach (var b in brands)
            {
                model.BrandSections.Add(new BrandProductSection
                {
                    BrandName = b.MaNcc, // Tên hãng
                    BrandLogo = b.Logo,      // Ní nhớ thêm cột Logo vào DB nếu chưa có
                    Products = await _context.HangHoas
                        .Where(h => h.MaNcc == b.MaNcc)
                        .Take(5)
                        .Select(h => new HangHoaVM
                        {
                            MaHh = h.MaHh,
                            TenHh = h.TenHh,
                            Hinh = h.Hinh,
                            DonGia = h.DonGia ?? 0,
                            GiamGia = h.GiamGia ?? 0,
                            DiemDanhGia = (int)(h.DiemDanhGia ?? 5),
                            SoLuongBan = h.SoLuongBan ?? 0,
                        }).ToListAsync()
                });
            }
            // 8 top bán chạy của tháng
            // Lấy Top 8 sản phẩm bán chạy nhất để làm "Sản phẩm của tháng"
            model.MonthlyProducts = await _context.HangHoas
                //.Where(h => h.IsActive) // Đảm bảo còn hàng/đang bán
                .OrderByDescending(h => h.SoLuongBan) // Sắp xếp theo lượt bán
                .Take(10)
                .Select(h => new HangHoaVM
                {
                    MaHh = h.MaHh,
                    TenHh = h.TenHh,
                    Hinh = h.Hinh,
                    DonGia = h.DonGia ?? 0,
                    GiamGia = h.GiamGia ?? 0,
                    DiemDanhGia = (int)(h.DiemDanhGia ?? 5),
                    SoLuongBan = h.SoLuongBan ?? 0,
                    IsHot = true // Thêm một cờ hiệu để hiện badge
                }).ToListAsync();
            return View(model);
        }

        // --- HÀM PHỤ (HELPER METHODS) ---

        // Hàm bóc tách YouTube ID siêu đa năng (Xử lý cả Shorts, Watch, và link rút gọn)
        // HÀM HELPER "VẠN NĂNG": Xử lý mọi loại link Youtube
        private string GetYouTubeId(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";

            if (url.Contains("/shorts/"))
                return url.Split("/shorts/")[1].Split('?')[0];

            if (url.Contains("v="))
                return url.Split("v=")[1].Split('&')[0];

            if (url.Contains("youtu.be/"))
                return url.Split("youtu.be/")[1].Split('?')[0];

            if (url.Contains("/embed/"))
                return url.Split("/embed/")[1].Split('?')[0];

            return url; // Nếu là ID sẵn thì trả về luôn
        }

        private async Task<List<HangHoaVM>> GetProductsByCategory(int maLoai, int take)
        {
            return await _context.HangHoas
                .Where(h => h.MaLoai == maLoai)
                .OrderByDescending(h => h.MaHh)
                .Take(take)
                .Select(h => new HangHoaVM
                {
                    MaHh = h.MaHh,
                    TenHh = h.TenHh,
                    Hinh = h.Hinh,
                    DonGia = h.DonGia ?? 0,
                    GiamGia = h.GiamGia ?? 0,
                    DiemDanhGia = (int)(h.DiemDanhGia ?? 5),
                    SoLuongBan = h.SoLuongBan ?? 0,
                    TenLoai = h.MaLoaiNavigation.TenLoai
                }).ToListAsync();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
