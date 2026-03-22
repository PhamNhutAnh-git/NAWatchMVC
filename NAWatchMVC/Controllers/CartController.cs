using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.Data;
using NAWatchMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Helpers; // Nhớ using Helper để dùng Session
using Microsoft.Extensions.Configuration; // <--- Thêm dòng này nếu thấy IConfiguration báo đỏ
namespace NAWatchMVC.Controllers
{
    // [Authorize] <-- Đã bỏ để khách vãng lai vào được
    public class CartController : Controller
    {
        private readonly NawatchMvcContext db;
        private readonly IConfiguration _configuration; // <--- THÊM BIẾN NÀY VNP
        const string CART_KEY = "MYCART";
        //private readonly MyEmailSender _emailSender; // <--- KHAI BÁO
        // Helper để lấy giỏ hàng từ Session nhanh gọn
        public List<CartItemVM> Cart => HttpContext.Session.Get<List<CartItemVM>>(CART_KEY) ?? new List<CartItemVM>();

        public CartController(NawatchMvcContext context, MyEmailSender emailSender, IConfiguration configuration)
        {
            db = context;
            //_emailSender = emailSender;// <--- GÁN gmail
            _configuration = configuration; // <--- GÁN GIÁ TRỊ VNP
        }

        // 1. XEM GIỎ HÀNG
        public IActionResult Index()
        {

            // --- CHẶN ADMIN MUA HÀNG ---
            if (User.IsInRole("Admin") || User.IsInRole("Staff"))
            {
                return View("AdminCantShop");
            }
            // ----------------------------
            List<CartItemVM> result;

            if (User.Identity.IsAuthenticated)
            {
                // --- KHÁCH QUEN: Lấy từ Database ---
                var maKH = User.FindFirst("CustomerId")?.Value;
                var gioHang = db.GioHangs
                    .Include(gh => gh.ChiTietGioHangs).ThenInclude(ct => ct.MaHhNavigation)
                    .FirstOrDefault(gh => gh.MaKh == maKH);

                if (gioHang == null) result = new List<CartItemVM>();
                else
                {
                    result = gioHang.ChiTietGioHangs.Select(item => new CartItemVM
                    {
                        MaHH = item.MaHh,
                        TenHH = item.MaHhNavigation.TenHh,
                        Hinh = item.MaHhNavigation.Hinh ?? "",
                        DonGia = item.MaHhNavigation.DonGia ?? 0, // Gán giá gốc
                        GiamGia = item.MaHhNavigation.GiamGia ?? 0, // Gán phần trăm giảm
                        SoLuong = item.SoLuong ?? 0
                    }).ToList();
                }
            }
            else
            {
                // --- KHÁCH VÃNG LAI: Lấy từ Session ---
                result = Cart;
            }

            return View("Index", result);
        }

        // 2. THÊM VÀO GIỎ HÀNG (HYBRID)
        [HttpPost]
        [Route("Cart/Add/{id}")] // Thêm dòng này để khớp với URL: /Cart/Add/117
        public IActionResult AddToCart(int id, int quantity = 1)
        {
            // 1. Lấy thông tin hàng hóa để kiểm tra Tồn kho
            var hangHoa = db.HangHoas.Find(id);
            if (hangHoa == null) return NotFound();
            // 2. TÍNH GIÁ BÁN THỰC TẾ (QUAN TRỌNG)
            double donGiaThucTe = hangHoa.DonGia ?? 0;
            if (hangHoa.GiamGia > 0)
            {
                // GIẢI THÍCH: 
                // 1. Dùng ?? 0 để tránh lỗi Null.
                // 2. Chia cho 100.0 để biến số nguyên (10) thành số thập phân (0.1).
                donGiaThucTe = donGiaThucTe * (1 - (hangHoa.GiamGia ?? 0) / 100.0);
            }
            // -------------------------------------
            // --- [FIX LỖI] KIỂM TRA TỒN KHO ---
            if (hangHoa.SoLuong <= 0)
            {
                return Json(new { error = true, message = "Sản phẩm này đã hết hàng." });
            }
            if (hangHoa.SoLuong < quantity)
            {
                return Json(new { error = true, message = $"Kho chỉ còn {hangHoa.SoLuong} sản phẩm." });
            }
            // -----------------------------------
            // --- CHẶN ADMIN MUA HÀNG ---
            if (User.IsInRole("Admin") || User.IsInRole("Staff"))
            {
                return View("AdminCantShop");
            }
            // ----------------------------
            // 2. Khai báo biến chung (để dùng được ở cả đoạn dưới)
            var maKH = User.FindFirst("CustomerId")?.Value;
            // TRƯỜNG HỢP 1: ĐÃ ĐĂNG NHẬP (Lưu DB)
            if (User.Identity.IsAuthenticated)
            {
                // code giỏ hàng
                var gioHang = db.GioHangs.FirstOrDefault(gh => gh.MaKh == maKH);
                if (gioHang == null)
                {
                    gioHang = new GioHang { MaKh = maKH, NgayTao = DateTime.Now };
                    db.GioHangs.Add(gioHang);
                    db.SaveChanges();
                }
                // code chi tiết 
                var chiTiet = db.ChiTietGioHangs.FirstOrDefault(ct => ct.MaGh == gioHang.MaGh && ct.MaHh == id);
                if (chiTiet == null)
                {
                    chiTiet = new ChiTietGioHang { MaGh = gioHang.MaGh, MaHh = id, SoLuong = quantity, NgayThem = DateTime.Now };
                    db.ChiTietGioHangs.Add(chiTiet);
                }
                else
                {
                    chiTiet.SoLuong += quantity;
                    
                }
                db.SaveChanges();
            }
            // TRƯỜNG HỢP 2: KHÁCH VÃNG LAI (Lưu Session)
            else
            {
                var myCart = Cart;
                var item = myCart.SingleOrDefault(p => p.MaHH == id);
                if (item == null)
                {
                    //var hangHoa = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
                    if (hangHoa == null) return NotFound();

                    item = new CartItemVM
                    {
                        MaHH = hangHoa.MaHh,
                        TenHH = hangHoa.TenHh,
                        Hinh = hangHoa.Hinh ?? "",
                        // Gán dữ liệu gốc, VM sẽ tự dùng công thức để hiện GiaBan và ThanhTien
                        DonGia = hangHoa.DonGia ?? 0,
                        GiamGia = (int)(hangHoa.GiamGia ?? 0),

                        SoLuong = quantity
                    };
                    myCart.Add(item);
                }
                else
                {
                    item.SoLuong += quantity;
                }
                HttpContext.Session.Set(CART_KEY, myCart);
            }
            // --- ĐOẠN CUỐI HÀM ADDTOCART: PHÂN LUỒNG XỬ LÝ ---
            // 1. Kiểm tra loại yêu cầu (AJAX hay Normal Post)
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            // 2. Nếu là AJAX (Dành cho nút "Thêm vào giỏ hàng")
            if (isAjax)
            {
                // --- PHẦN TÍNH TỔNG SỐ LƯỢNG (GIỮ NGUYÊN BIẾN CỦA NÍ) ---
                int tongSoLuong = 0;
                if (User.Identity.IsAuthenticated)
                {
                    // maKH ní đã lấy ở đoạn trên của hàm
                    var gioHang = db.GioHangs.Include(gh => gh.ChiTietGioHangs).FirstOrDefault(gh => gh.MaKh == maKH);
                    if (gioHang != null)
                    {
                        tongSoLuong = gioHang.ChiTietGioHangs.Sum(c => c.SoLuong ?? 0);
                    }
                }
                else
                {
                    tongSoLuong = Cart.Sum(c => c.SoLuong);
                }
                // --- BƯỚC CUỐI: TRẢ VỀ JSON CHO AJAX ---
                // Sử dụng hangHoa.TenHh hoặc tenSP tùy ní đặt tên ở trên nhé
                return Ok(new
                {
                    success = true,
                    soLuong = tongSoLuong,
                    tenHH = hangHoa.TenHh
                });
            }
            // --- XỬ LÝ RIÊNG CHO MUA NGAY ---
            // Lưu ID và Số lượng món vừa nhấn "Mua ngay" vào Session để Checkout biết đường mà lọc
            HttpContext.Session.SetInt32("BuyNow_Id", id);
            HttpContext.Session.SetInt32("BuyNow_Quantity", quantity);
            return RedirectToAction("Checkout", "Cart");
        }

        // 3. XÓA SẢN PHẨM khỏi giỏ
        public IActionResult RemoveCart(int id)
        {
            if (User.Identity.IsAuthenticated)
            {
                var maKH = User.FindFirst("CustomerId")?.Value;
                var gioHang = db.GioHangs.FirstOrDefault(gh => gh.MaKh == maKH);
                if (gioHang != null)
                {
                    var chiTiet = db.ChiTietGioHangs.FirstOrDefault(ct => ct.MaGh == gioHang.MaGh && ct.MaHh == id);
                    if (chiTiet != null)
                    {
                        db.ChiTietGioHangs.Remove(chiTiet);
                        db.SaveChanges();
                    }
                }
            }
            else
            {
                var myCart = Cart;
                var item = myCart.SingleOrDefault(p => p.MaHH == id);
                if (item != null)
                {
                    myCart.Remove(item);
                    HttpContext.Session.Set(CART_KEY, myCart);
                }
            }
            return RedirectToAction("Index");
        }

        // 4. TĂNG SỐ LƯỢNG cho giỏ
        public IActionResult TangSoLuong(int id)
        {
            if (User.Identity.IsAuthenticated)
            {
                var maKH = User.FindFirst("CustomerId")?.Value;
                var gioHang = db.GioHangs.FirstOrDefault(gh => gh.MaKh == maKH);
                if (gioHang != null)
                {
                    var chiTiet = db.ChiTietGioHangs.FirstOrDefault(ct => ct.MaGh == gioHang.MaGh && ct.MaHh == id);
                    if (chiTiet != null)
                    {
                        chiTiet.SoLuong++;
                        db.SaveChanges();
                    }
                }
            }
            else
            {
                var myCart = Cart;
                var item = myCart.SingleOrDefault(p => p.MaHH == id);
                if (item != null)
                {
                    item.SoLuong++;
                    HttpContext.Session.Set(CART_KEY, myCart);
                }
            }
            return RedirectToAction("Index");
        }

        // 5. GIẢM SỐ LƯỢNG cho giỏ
        public IActionResult GiamSoLuong(int id)
        {
            if (User.Identity.IsAuthenticated)
            {
                var maKH = User.FindFirst("CustomerId")?.Value;
                var gioHang = db.GioHangs.FirstOrDefault(gh => gh.MaKh == maKH);
                if (gioHang != null)
                {
                    var chiTiet = db.ChiTietGioHangs.FirstOrDefault(ct => ct.MaGh == gioHang.MaGh && ct.MaHh == id);
                    if (chiTiet != null && chiTiet.SoLuong > 1)
                    {
                        chiTiet.SoLuong--;
                        db.SaveChanges();
                    }
                }
            }
            else
            {
                var myCart = Cart;
                var item = myCart.SingleOrDefault(p => p.MaHH == id);
                if (item != null && item.SoLuong > 1)
                {
                    item.SoLuong--;
                    HttpContext.Session.Set(CART_KEY, myCart);
                }
            }
            return RedirectToAction("Index");
        }

        // 6. HIỂN THỊ CHECKOUT (GET)
        [HttpGet]
        public IActionResult Checkout()
        {
            // 1. CHẶN ADMIN MUA HÀNG (Giữ nguyên logic của ní)
            if (User.IsInRole("Admin") || User.IsInRole("Staff"))
            {
                return View("AdminCantShop");
            }

            // 2. LẤY THÔNG TIN KHÁCH HÀNG (Nếu đã đăng nhập)
            if (User.Identity.IsAuthenticated)
            {
                var maKH = User.FindFirst("CustomerId")?.Value;
                var khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == maKH);
                if (khachHang != null) ViewBag.ThongTinKhachHang = khachHang;
            }

            // 3. KHỞI TẠO DANH SÁCH KẾT QUẢ
            List<CartItemVM> result = new List<CartItemVM>();

            // --- BƯỚC QUAN TRỌNG: KIỂM TRA MUA NGAY TRƯỚC ---
            var buyNowId = HttpContext.Session.GetInt32("BuyNow_Id");
            var buyNowQty = HttpContext.Session.GetInt32("BuyNow_Quantity") ?? 1;

            if (buyNowId.HasValue)
            {
                // TRƯỜNG HỢP 1: MUA NGAY -> Chỉ lấy đúng 1 món này
                var hangHoa = db.HangHoas.Find(buyNowId.Value);
                if (hangHoa != null)
                {
                    result.Add(new CartItemVM
                    {
                        MaHH = hangHoa.MaHh,
                        TenHH = hangHoa.TenHh,
                        Hinh = hangHoa.Hinh ?? "",
                        DonGia = hangHoa.DonGia ?? 0,
                        GiamGia = (int)(hangHoa.GiamGia ?? 0),
                        SoLuong = buyNowQty
                    });
                }
                // Lưu ý: Đừng xóa Session vội ở đây để đề phòng khách F5 trang
            }
            else
            {
                // TRƯỜNG HỢP 2: THANH TOÁN CẢ GIỎ HÀNG (Logic cũ của ní)
                if (User.Identity.IsAuthenticated)
                {
                    var maKH = User.FindFirst("CustomerId")?.Value;
                    var gioHang = db.GioHangs.Include(gh => gh.ChiTietGioHangs)
                                             .ThenInclude(ct => ct.MaHhNavigation)
                                             .FirstOrDefault(gh => gh.MaKh == maKH);
                    if (gioHang != null)
                    {
                        result = gioHang.ChiTietGioHangs.Select(item => new CartItemVM
                        {
                            MaHH = item.MaHh,
                            TenHH = item.MaHhNavigation.TenHh,
                            Hinh = item.MaHhNavigation.Hinh ?? "",
                            DonGia = item.MaHhNavigation.DonGia ?? 0,
                            SoLuong = item.SoLuong ?? 1,
                            GiamGia = item.MaHhNavigation.GiamGia ?? 0
                        }).ToList();
                    }
                }
                else
                {
                    result = Cart; // Lấy từ Session khách vãng lai
                }
                // --- KIỂM TRA TỒN KHO LẦN CUỐI CHO CẢ GIỎ cho trường hợp để lâu---
                foreach (var item in result)
                {
                    var sp = db.HangHoas.AsNoTracking().FirstOrDefault(h => h.MaHh == item.MaHH);
                    if (sp == null || sp.SoLuong < item.SoLuong)
                    {
                        TempData["Message"] = $"Sản phẩm {item.TenHH} vừa thay đổi số lượng tồn kho. Vui lòng kiểm tra lại giỏ hàng.";
                        return RedirectToAction("Index");
                    }
                }
            }

            // 4. KIỂM TRA RỖNG
            if (result.Count == 0) return RedirectToAction("Index");

            return View(result);
        }

        // 7. XỬ LÝ CHECKOUT (POST)
        [HttpPost]
        public async Task<IActionResult> Checkout(string email, string hoten, string dienthoai, string diachi, string ghichu, string CachThanhToan)
        {
            KhachHang khachHang = null;

            // A. Xử lý khách hàng
            if (User.Identity.IsAuthenticated)
            {
                var maKH = User.FindFirst("CustomerId")?.Value;
                khachHang = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == maKH);
                if (khachHang != null)
                {
                    // Cập nhật địa chỉ mới nhất
                    khachHang.DiaChi = diachi;
                    khachHang.DienThoai = dienthoai;
                    db.Update(khachHang);
                    await db.SaveChangesAsync();
                }
            }
            if (khachHang == null) // Khách vãng lai
            {
                var khachHangCu = db.KhachHangs.SingleOrDefault(k => k.Email == email);
                if (khachHangCu == null)
                {
                    khachHang = new KhachHang
                    {
                        MaKh = email,
                        Email = email,
                        HoTen = hoten,
                        DienThoai = dienthoai,
                        DiaChi = diachi,
                        MatKhau = Guid.NewGuid().ToString(),
                        VaiTro = 2,
                        HieuLuc = true,
                        RandomKey = Guid.NewGuid().ToString()
                    };
                    db.Add(khachHang);
                    await db.SaveChangesAsync();
                }
                else
                {
                    khachHang = khachHangCu;
                    khachHang.HoTen = hoten; khachHang.DiaChi = diachi; khachHang.DienThoai = dienthoai;
                    db.Update(khachHang);
                    await db.SaveChangesAsync();
                }
            }
            // B. Lấy giỏ hàng (Từ DB hoặc Session)
            List<CartItemVM> myCart = new List<CartItemVM>();
            var buyNowId = HttpContext.Session.GetInt32("BuyNow_Id");
            var buyNowQty = HttpContext.Session.GetInt32("BuyNow_Quantity") ?? 1;

            if (buyNowId.HasValue)
            {
                // TRƯỜNG HỢP 1: MUA NGAY -> Chỉ hốt 1 món này thôi
                var hh = db.HangHoas.Find(buyNowId.Value);
                if (hh != null)
                {
                    myCart.Add(new CartItemVM
                    {
                        MaHH = hh.MaHh,
                        DonGia = hh.DonGia ?? 0,
                        SoLuong = buyNowQty,
                        GiamGia = (int)(hh.GiamGia ?? 0)
                    });
                }
            }
            else
            {
                // TRƯỜNG HỢP 2: THANH TOÁN CẢ GIỎ (Giữ nguyên logic cũ của ní)
                if (User.Identity.IsAuthenticated)
                {
                    var gioHang = db.GioHangs.Include(gh => gh.ChiTietGioHangs).FirstOrDefault(gh => gh.MaKh == khachHang.MaKh);
                    if (gioHang != null)
                        myCart = gioHang.ChiTietGioHangs.Select(ct => new CartItemVM { MaHH = ct.MaHh, DonGia = db.HangHoas.Find(ct.MaHh).DonGia ?? 0, SoLuong = ct.SoLuong ?? 1 }).ToList();
                }
                else
                {
                    myCart = Cart;
                }
            }

            // C. Tạo hóa đơn
            if (myCart != null && myCart.Count > 0)
            {
                // --- BƯỚC MỚI: KIỂM TRA TỒN KHO TỔNG THỂ TRƯỚC KHI LƯU ---
                foreach (var item in myCart)
                {
                    var hh = db.HangHoas.AsNoTracking().FirstOrDefault(h => h.MaHh == item.MaHH);
                    if (hh == null || hh.SoLuong < item.SoLuong)
                    {
                        // Nếu có 1 món không đủ, báo lỗi và dừng toàn bộ tiến trình
                        TempData["Message"] = $"Sản phẩm {hh?.TenHh} vừa hết hàng hoặc không đủ số lượng. Vui lòng kiểm tra lại!";
                        return RedirectToAction("Index");
                    }
                }
                //1.Tạo Hóa Đơn(Lưu vào DB trước để lấy MaHD)
                var hoadon = new HoaDon
                {
                    MaKh = khachHang.MaKh,
                    NgayDat = DateTime.Now,
                    TongTien = 0, // Sẽ cập nhật lại bên dưới
                    MaTrangThai = 0,
                    HoTen = hoten,
                    DiaChi = diachi,
                    DienThoai = dienthoai,
                    GhiChu = ghichu,
                    CachThanhToan = CachThanhToan, // SỬA: Lấy từ tham số truyền vào (VNPAY/COD)
                    CachVanChuyen = "GHN",
                    PhiVanChuyen = 0,
                    DaThanhToan = false     // SỬA: Mặc định là false, VNPay thành công mới chuyển true
                };

                db.Add(hoadon);
                await db.SaveChangesAsync();// lưu để có mã hd
                // 2. Lưu chi tiết và tính tổng tiền thực tế
                double tongTienThucTe = 0;
                // Lưu Chi tiết & Trừ kho Trù kho khi vừa tạo xong hóa đơn
                    foreach (var item in myCart)
                    {
                        // Lấy thông tin mới nhất từ DB để đảm bảo giá và tồn kho chính xác lúc nhấn nút
                        var hh = db.HangHoas.Find(item.MaHH);
                        if (hh == null) continue;

                        // TÍNH GIÁ BÁN THỰC TẾ (Vì GiamGia là số nguyên nên phải chia 100.0)
                        double giaGoc = (double)(hh.DonGia ?? 0);
                        int phanTramGiam = (int)(hh.GiamGia ?? 0);
                        double giaBan = giaGoc * (1 - phanTramGiam / 100.0); // Công thức: $GiaGoc \times (1 - \frac{GiamGia}{100})$

                        // Tạo bản ghi Chi tiết hóa đơn để "Chốt giá"
                        var chiTiet = new ChiTietHd
                        {
                            MaHd = hoadon.MaHd,
                            MaHh = item.MaHH,
                            DonGia = giaBan,         // LƯU GIÁ ĐÃ GIẢM: Để sau này sản phẩm có đổi giá thì hóa đơn cũ không bị đổi theo
                            SoLuong = item.SoLuong,
                            GiamGia = phanTramGiam   // Lưu % giảm tại thời điểm mua để làm bằng chứng đối soát
                        };
                        db.ChiTietHds.Add(chiTiet);

                        // Cộng dồn vào tổng tiền thực tế của cả hóa đơn
                        tongTienThucTe += (giaBan * item.SoLuong);

                        // TRỪ KHO (Cập nhật số lượng còn lại trong bảng HangHoa)
                        if (hh.SoLuong >= item.SoLuong)
                        {
                            hh.SoLuong -= item.SoLuong;
                        }
                        else
                        {
                            // Nếu lúc khách nhấn nút mà kho vừa hết, ní có thể xử lý báo lỗi ở đây
                            hh.SoLuong = 0;
                        }
                        // === THÊM DÒNG NÀY VÀO ĐỂ TĂNG SỐ LƯỢNG BÁN ===
                        // Dùng ?? 0 để đề phòng trường hợp cột SoLuongBan trong DB đang bị NULL
                        hh.SoLuongBan = (hh.SoLuongBan ?? 0) + item.SoLuong;

                        // Đánh dấu là đối tượng HangHoa này đã thay đổi để EF Core biết đường mà UPDATE
                        db.Entry(hh).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    }
                // Bước C: Cập nhật lại Tổng tiền cho Hóa đơn
                hoadon.TongTien = tongTienThucTe;
                db.Update(hoadon);
                await db.SaveChangesAsync(); // Lưu lần cuối
                // D. Xóa giỏ hàng (Đã đặt xong thì xóa giỏ)
                if (buyNowId.HasValue)
                {
                    // Nếu là Mua ngay: CHỈ xóa cờ Mua ngay, giữ nguyên giỏ hàng trong DB/Session
                    HttpContext.Session.Remove("BuyNow_Id");
                    HttpContext.Session.Remove("BuyNow_Quantity");
                }
                else
                {
                    // Nếu là thanh toán cả giỏ: Xóa sạch sành sanh
                    if (User.Identity.IsAuthenticated)
                    {
                        var gioHang = db.GioHangs.FirstOrDefault(gh => gh.MaKh == khachHang.MaKh);
                        if (gioHang != null)
                        {
                            var chiTiets = db.ChiTietGioHangs.Where(ct => ct.MaGh == gioHang.MaGh);
                            db.ChiTietGioHangs.RemoveRange(chiTiets);
                            await db.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        HttpContext.Session.Remove("MYCART");
                    }
                }
                // E. PHÂN LUỒNG THANH TOÁN (VNPAY vs COD)
                if (CachThanhToan == "VNPAY")
                {
                    var vnPayModel = new NAWatchMVC.Helpers.VnPayLibrary();

                    // 1. THÔNG SỐ CỐ ĐỊNH (Điền tay sạch sẽ)
                    string hashSecret = "1LC4SIJ4JWG55XDFIASTLM5212O7J7L5";
                    string baseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

                    // 2. THAM SỐ BẮT BUỘC (Tinh gọn nhất có thể)
                    vnPayModel.AddRequestData("vnp_Version", "2.1.0");
                    vnPayModel.AddRequestData("vnp_Command", "pay");
                    vnPayModel.AddRequestData("vnp_TmnCode", "BO2N8J7E");

                    // Số tiền: Nhân 100 và ép kiểu long để không có số thập phân
                    long amount = (long)(hoadon.TongTien * 100);
                    vnPayModel.AddRequestData("vnp_Amount", amount.ToString());

                    vnPayModel.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    vnPayModel.AddRequestData("vnp_CurrCode", "VND");
                    vnPayModel.AddRequestData("vnp_IpAddr", "127.0.0.1"); // Fix cứng IP local
                    vnPayModel.AddRequestData("vnp_Locale", "vn");
                    vnPayModel.AddRequestData("vnp_OrderInfo", "ThanhToan"); // Viết liền, không dấu, không cách
                    vnPayModel.AddRequestData("vnp_OrderType", "other");
                    vnPayModel.AddRequestData("vnp_ReturnUrl", "https://localhost:7116/Cart/PaymentCallBack");
                    vnPayModel.AddRequestData("vnp_TxnRef", hoadon.MaHd.ToString());
                    // Đừng điền NCB, hãy để trống hoặc không truyền để nó hiện danh sách ngân hàng
                    vnPayModel.AddRequestData("vnp_BankCode", "");
                    // 3. TẠO URL VÀ REDIRECT
                    string paymentUrl = vnPayModel.CreateRequestUrl(baseUrl, hashSecret);

                    // In ra cửa sổ Output để mình soi nếu vẫn lỗi
                    System.Diagnostics.Debug.WriteLine("=== FINAL DEBUG URL ===");
                    System.Diagnostics.Debug.WriteLine(paymentUrl);

                    return Redirect(paymentUrl);
                }

                // Nếu là COD -> Về trang thành công luôn
                return View("Success", hoadon);
            }

            return RedirectToAction("Index");
        }
        //8. XỬ LÝ KẾT QUẢ TRẢ VỀ TỪ VNPAY
        [HttpGet]
        public async Task<IActionResult> PaymentCallBack()
        {
            if (Request.Query.Count == 0)
            {
                return RedirectToAction("Index");
            }

            // Lấy các thông số trả về từ VNPAY
            var vnpayData = Request.Query;
            var vnPayLibrary = new NAWatchMVC.Helpers.VnPayLibrary();

            foreach (var (key, value) in vnpayData)
            {
                // Lấy tất cả dữ liệu bắt đầu bằng vnp_
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnPayLibrary.AddResponseData(key, value.ToString());
                }
            }

            // Lấy mã đơn hàng (vnp_TxnRef) và mã giao dịch
            string vnp_TxnRef = vnPayLibrary.GetResponseData("vnp_TxnRef");
            string vnp_ResponseCode = vnPayLibrary.GetResponseData("vnp_ResponseCode");
            string vnp_SecureHash = vnpayData["vnp_SecureHash"];

            // Lấy mã bí mật từ cấu hình
            string vnp_HashSecret = _configuration["VnPay:HashSecret"]; // Đảm bảo trong appsettings.json đã có

            // Kiểm tra chữ ký bảo mật (để đảm bảo dữ liệu không bị fake)
            bool checkSignature = vnPayLibrary.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (checkSignature)
            {
                // 1. Lôi MaHD và hoaDon ra nằm ngoài cái if thành công/thất bại
                var maHD = int.Parse(vnp_TxnRef);

                // QUAN TRỌNG: Phải có .Include(h => h.ChiTietHds) thì lát nữa ní mới có dữ liệu để cộng lại kho nhé
                var hoaDon = db.HoaDons
                               .Include(h => h.ChiTietHds)
                               .FirstOrDefault(x => x.MaHd == maHD);
                if (vnp_ResponseCode == "00") // 00: Giao dịch thành công
                {
                    if (hoaDon != null)
                    {
                        hoaDon.MaTrangThai = 1; // 1: Đã thanh toán chờ xác nhận. / Đang xử lý (Tùy quy ước của bạn)
                        hoaDon.DaThanhToan = true; // ĐÃ THU TIỀN XONG                        
                        db.Update(hoaDon);
                        await db.SaveChangesAsync();

                        // Gửi Email xác nhận (Nếu cần)
                        // await _emailSender.SendEmailAsync(hoaDon.MaKhNavigation.Email, "Thanh toán thành công", "...");
                    }

                    return View("PaymentSuccess");
                }
                else
                {
                    // THANH TOÁN THẤT BẠI HOẶC KHÁCH BẤM HỦY (Mã 24)
                    if (hoaDon != null)
                    {
                        // 1. Chuyển trạng thái hóa đơn thành Hủy (Mã 4)
                        hoaDon.MaTrangThai = 4;
                        hoaDon.GhiChu += " | Khách đã hủy thanh toán hoặc lỗi giao dịch.";

                        // 2. CỘNG LẠI KHO: Vì lúc Checkout mình đã trừ, giờ không trả tiền thì phải trả hàng lại
                        foreach (var item in hoaDon.ChiTietHds)
                        {
                            var sp = db.HangHoas.Find(item.MaHh);
                            if (sp != null)
                            {
                                sp.SoLuong += item.SoLuong; // Trả lại số lượng vào kho
                                sp.SoLuongBan -= item.SoLuong; // Trừ lại số lượng đã bán ảo
                            }
                        }

                        db.Update(hoaDon);
                        await db.SaveChangesAsync();
                    }
                    ViewBag.Message = "Thanh toán không thành công. Mã lỗi: " + vnp_ResponseCode;
                    return View("PaymentCanceled");
                }
            }
            else
            {
                // Sai chữ ký (Có thể do hacker can thiệp)
                ViewBag.Message = "Có lỗi xảy ra trong quá trình xử lý (Sai chữ ký)";
                return View("PaymentFail");
            }
        }

        // 9. KHÁCH TỰ HỦY ĐƠN (CHỈ ÁP DỤNG MÃ 0)
        [HttpGet]
        public async Task<IActionResult> CancelOrder(int id)
        {
            // 1. Tìm hóa đơn và phải lôi cả Chi tiết ra để biết đường mà cộng lại kho
            var hoadon = db.HoaDons.Include(h => h.ChiTietHds).FirstOrDefault(h => h.MaHd == id);

            if (hoadon == null) return NotFound();

            // 2. Kiểm tra bảo mật: Chỉ cho phép hủy nếu trạng thái là 0 (Mới đặt/COD)
            // Nếu là mã 1 (VNPay) hoặc 2 (Đang giao) thì không cho vào đây
            if (hoadon.MaTrangThai != 0)
            {
                TempData["Message"] = "Đơn này không thể tự hủy. Ní vui lòng liên hệ Admin nha!";
                return RedirectToAction("Index", "HoaDon"); // Quay lại trang lịch sử đơn hàng
            }

            // 3. CỘNG LẠI SỐ LƯỢNG VÀO KHO
            foreach (var item in hoadon.ChiTietHds)
            {
                var sp = db.HangHoas.Find(item.MaHh);
                if (sp != null)
                {
                    sp.SoLuong += item.SoLuong; // Trả lại hàng vào kho
                    sp.SoLuongBan = (sp.SoLuongBan ?? 0) - item.SoLuong; // Trừ lại số lượng bán ảo
                    db.Update(sp);
                }
            }

            // 4. CẬP NHẬT TRẠNG THÁI HÓA ĐƠN THÀNH 4 (HỦY ĐƠN)
            hoadon.MaTrangThai = 4;
            hoadon.GhiChu += " | Khách hàng chủ động hủy đơn.";

            db.Update(hoadon);
            await db.SaveChangesAsync();

            TempData["Message"] = "Đã hủy đơn hàng thành công và trả hàng về kho!";
            // Sửa lại cho đúng tên Action và Controller mà ní đang dùng để hiện danh sách đơn
            return RedirectToAction("LichSuDonHang", "KhachHang");
        }


    }
}