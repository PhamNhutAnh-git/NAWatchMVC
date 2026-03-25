using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.Data;          // Đã đổi sang namespace mới
using NAWatchMVC.ViewModels;    // Đã đổi sang namespace mới
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using NAWatchMVC.Helpers;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Hosting;

namespace NAWatchMVC.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly NawatchMvcContext db; // Đã đổi tên Context
        private readonly IPasswordHasher<KhachHang> _passwordHasher;
        private readonly MyEmailSender _emailSender;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public KhachHangController(IWebHostEnvironment webHostEnvironment,NawatchMvcContext context, IPasswordHasher<KhachHang> passwordHasher, MyEmailSender emailSender)
        {
            _webHostEnvironment = webHostEnvironment;
            db = context;
            _passwordHasher = passwordHasher;
            _emailSender = emailSender;
        }

        #region Đăng Ký
        [HttpGet]
        public IActionResult DangKy() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(RegisterVM model, string? ReturnUrl)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra trùng TenDangNhap (Thay cho MaKh cũ của ní)
                if (db.KhachHangs.Any(kh => kh.TenDangNhap == model.TenDangNhap))
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã có người sử dụng vui lòng chọn tên khác.");
                    return View(model);
                }

                // 2. Kiểm tra trùng Email
                if (db.KhachHangs.Any(kh => kh.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký tài khoản.");
                    ModelState.AddModelError("Email", "Nếu bạn đã mua hàng bằng email này mà chưa đăng ký tài khoản thì tiến hành lấy mật khẩu lại nhé!!");
                    return View(model);
                }

                // 3. Tạo khách hàng mới
                var khachHang = new KhachHang
                {
                    
                    MaKh = "KH" + model.TenDangNhap.Trim(),//Tự động sinh MaKh theo công thức KH + Ten đăng nhập
                    TenDangNhap = model.TenDangNhap, // Tên đăng nhập do khách tự chọn
                    MatKhau = _passwordHasher.HashPassword(null, model.MatKhau),
                    HoTen = model.HoTen,
                    GioiTinh = model.GioiTinh,
                    NgaySinh = model.NgaySinh ?? DateTime.Now,
                    DiaChi = model.DiaChi,
                    DienThoai = model.DienThoai,
                    Email = model.Email,
                    Hinh = "default.jpg",
                    HieuLuc = true,
                    VaiTro = 1, // 1: Khách đã đăng ký (như ní muốn)
                    RandomKey = Guid.NewGuid().ToString() // Token dùng cho Quên mật khẩu
                };
                // 4. Lưu vào Database
                db.Add(khachHang);
                db.SaveChanges();
                
                // 2. CHUẨN BỊ "NGUYÊN LIỆU" CLAIMS ĐỂ TỰ ĐĂNG NHẬP
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Email, khachHang.Email),
                    new Claim(ClaimTypes.Name, khachHang.HoTen),
                    new Claim("CustomerId", khachHang.MaKh), // CỰC KỲ QUAN TRỌNG: Để hàm Checkout hốt được ID này
                    new Claim(ClaimTypes.Role, "Customer")
                };

                // 3. GỌI CÁI HÀM "HỘ PHÁP" CỦA NÍ ĐÃ CÓ
                await SignInUser(claims, false); // false vì mới đăng ký chưa cần "Ghi nhớ"

                // 4. GỘP GIỎ HÀNG (Dắt đồ từ Session vào DB cho ông khách mới này)
                await MergeCart(khachHang.MaKh);

                // 5. CHỞ KHÁCH VỀ ĐÍCH (Checkout hoặc Trang chủ)
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }
                // 5. Thông báo và chuyển sang trang Đăng nhập
                TempData["Message"] = "Đăng ký thành công! Đăng nhập ngay cho nóng ní ơi.";
                return RedirectToAction("DangNhap");
            }
            return View(model);
        }
        #endregion

        #region Đăng Nhập
        // GET: Hiển thị form Đăng nhập
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }
        // POST: Xử lý Đăng nhập
        [HttpPost]
        public async Task<IActionResult> DangNhap(LoginVM model, string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            if (ModelState.IsValid)
            {
                
                // 1. KIỂM TRA BẢNG NHÂN VIÊN (ADMIN/STAFF)
                // Nhân viên dùng MaNV hoặc Email để đăng nhập vào ô "TenDangNhap"
                var nhanVien = await db.NhanViens.SingleOrDefaultAsync(nv =>
                    nv.MaNv == model.TenDangNhap || nv.Email == model.TenDangNhap);

                if (nhanVien != null)
                {
                    if (!nhanVien.HieuLuc)
                    {
                        ModelState.AddModelError("loi", "Tài khoản nhân viên của ní đã bị khóa.");
                    }
                    else // Check Pass Nhân Viên (Dùng PasswordHasher)
                    {
                        var ketQua = _passwordHasher.VerifyHashedPassword(null, nhanVien.MatKhau, model.MatKhau);
                        if (ketQua == PasswordVerificationResult.Success)
                        {
                            var claims = new List<Claim> 
                            {
                                new Claim(ClaimTypes.Email, nhanVien.Email),
                                new Claim(ClaimTypes.Name, nhanVien.HoTen),
                                new Claim("EmployeeId", nhanVien.MaNv.ToString()),
                                // VaiTro 1: Giám đốc (Admin), 2: Nhân viên (Staff)
                                new Claim(ClaimTypes.Role, nhanVien.VaiTro == 1 ? "Admin" : "Staff")
                            };

                            // Phân quyền
                            await SignInUser(claims, model.RememberMe);

                            // Nếu là Admin/Staff thì vào thẳng trang quản trị
                            return Redirect("/Admin/HomeAdmin");
                        }
                    }
                }
                // 2. KIỂM TRA BẢNG KHÁCH HÀNG (CUSTOMER/VIP)
                // Đặt cái này để soi xem User có tồn tại không và Pass bị gì
                //var testUser = await db.KhachHangs.FirstOrDefaultAsync(x => x.Email == model.TenDangNhap || x.TenDangNhap == model.TenDangNhap);

                //if (testUser == null)
                //{
                //    System.Diagnostics.Debug.WriteLine("--- LỖI: Không tìm thấy User này trong DB! ---");
                //}
                //else
                //{
                //    var check = _passwordHasher.VerifyHashedPassword(testUser, testUser.MatKhau, model.MatKhau);
                //    System.Diagnostics.Debug.WriteLine($"--- USER: {testUser.TenDangNhap} | PASS CHECK: {check} ---");
                //}
                // Khách hàng dùng TenDangNhap mới tạo để đăng nhập
                var khachHang = await db.KhachHangs.SingleOrDefaultAsync(kh => kh.TenDangNhap == model.TenDangNhap || kh.Email == model.TenDangNhap || kh.MaKh == model.TenDangNhap);
                if (khachHang == null)
                {
                    // Lỗi 1: Sai User/Email/MaKH
                    ModelState.AddModelError("loi", "Tài khoản không tồn tại bạn ơi!");
                }
                if (khachHang != null)
                {
                    if (!(khachHang.HieuLuc ?? true))
                    {
                        ModelState.AddModelError("loi", "Tài khoản của bạn bị khóa rồi, liên hệ Admin nhé!");
                    }
                    else // Check Pass Khách Hàng
                    {
                        var ketQua = _passwordHasher.VerifyHashedPassword(khachHang, khachHang.MatKhau, model.MatKhau);
                        //if (ketQua == PasswordVerificationResult.Success)
                        if (ketQua != PasswordVerificationResult.Failed)
                        {
                            var claims = new List<Claim> 
                            {
                                new Claim(ClaimTypes.Email, khachHang.Email),
                                new Claim(ClaimTypes.Name, khachHang.HoTen),
                                new Claim("CustomerId", khachHang.MaKh),
                                // VaiTro 2: VIP, còn lại (0 hoặc 1): Customer
                                new Claim(ClaimTypes.Role, khachHang.VaiTro == 2 ? "VIP" : "Customer")
                            };

                            await SignInUser(claims, model.RememberMe);
                            // --- ĐOẠN CODE MỚI XỬ LÝ GHI NHỚ ---
                            var authProperties = new AuthenticationProperties
                            {
                                // Nếu tick Ghi nhớ -> True (Lưu lâu dài), Ngược lại -> False (Tắt trình duyệt là mất)
                                IsPersistent = model.RememberMe,

                                // Nếu ghi nhớ, set hạn 30 ngày. Nếu không, set hạn ngắn (ví dụ 30 phút)
                                ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddMinutes(30) : DateTime.UtcNow.AddMinutes(30)
                            };
                            // GỘP GIỎ HÀNG: Chuyển hàng từ Session/Cookie vào Database
                            await MergeCart(khachHang.MaKh);

                            // Trả khách về trang họ đang xem dở hoặc trang cá nhân
                            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                                return Redirect(ReturnUrl);

                            return RedirectToAction("Profile", "KhachHang");
                        }
                        else
                        {
                            // Nếu chạy đến đây là sai bét nhè rồi
                            ModelState.AddModelError("loi", "Mật khẩu dùng đăng nhập không chính xác rồi bạn! Vui lòng dùng chức năng 'Quên mật khẩu' để tạo mới nhé");
                            //ModelState.AddModelError("loi", "Tài khoản này chưa thiết lập mật khẩu. Ní vui lòng dùng chức năng 'Quên mật khẩu' để tạo mới nhé!");
                        }
                    }
                }

                // Nếu chạy đến đây là sai bét nhè rồi
                //ModelState.AddModelError("loi", "Mật khẩu dùng đăng nhập không chính xác rồi bạn!");
            }
            return View(model);
        }
        #endregion

        // Hàm phụ giúp code sạch hơn
        private async Task SignInUser(List<Claim> claims, bool rememberMe)
        {
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = rememberMe };
            await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        private async Task MergeCart(string maKh)
        {
            // 1. Lấy giỏ hàng từ Session
            var sessionCart = HttpContext.Session.Get<List<CartItemVM>>("MYCART");
            if (sessionCart != null && sessionCart.Any())
            {
                // Logic gộp giỏ hàng của ní ở đây... (giữ nguyên như code cũ của bạn)
                var gioHangDB = db.GioHangs.FirstOrDefault(gh => gh.MaKh == maKh); // Sửa ở đây nè ní!
                if (gioHangDB == null)
                {
                    gioHangDB = new GioHang { MaKh = maKh, NgayTao = DateTime.Now }; // Sửa ở đây nữa
                    db.GioHangs.Add(gioHangDB);
                    await db.SaveChangesAsync();
                    //db.SaveChanges();
                }
                // 3. Duyệt danh sách từ Session để "đổ" vào Chi tiết giỏ hàng trong DB
                foreach (var itemS in sessionCart)
                {
                    var chiTiet = db.ChiTietGioHangs.FirstOrDefault(ct => ct.MaGh == gioHangDB.MaGh && ct.MaHh == itemS.MaHH);
                    if (chiTiet == null)
                    {
                        db.ChiTietGioHangs.Add(new ChiTietGioHang { MaGh = gioHangDB.MaGh, MaHh = itemS.MaHH, SoLuong = itemS.SoLuong, NgayThem = DateTime.Now });
                    }
                    else
                    {
                        chiTiet.SoLuong += itemS.SoLuong;
                    }
                }
                //db.SaveChanges();
                await db.SaveChangesAsync();
                HttpContext.Session.Remove("MYCART");
            }
        }
        // Action Đăng xuất
        [Authorize]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        // ... Các hàm Profile, LichSuDonHang giữ nguyên logic cũ ...
        #region Hồ Sơ Cá Nhân
        // Trang xem thông tin cá nhân
        [Authorize] // Bắt buộc phải đăng nhập mới vào được
        public IActionResult Profile()
        {
            // Lấy MaKh từ Claim lưu trong Cookie
            var maKh = User.FindFirst("CustomerId")?.Value;

            if (maKh == null)
            {
                return NotFound();
            }

            // Lấy thông tin khách hàng từ Database
            var myProfile = db.KhachHangs.SingleOrDefault(kh => kh.MaKh == maKh);

            return View(myProfile);
        }
        // Xem thông tin cá nhân dành cho NHÂN VIÊN
        [Authorize(Roles = "Admin,Staff")] // Chỉ nhân viên/admin mới vào được
        public IActionResult ProfileNhanVien()
        {
            // Lấy Mã Nhân Viên từ Claim (lúc đăng nhập đã lưu)
            var maNv = User.FindFirst("EmployeeId")?.Value;

            if (string.IsNullOrEmpty(maNv))
            {
                return RedirectToAction("DangNhap");
            }

            // Tìm trong bảng NhanVien (Chú ý: MaNv là string hay int tùy model của bạn)
            // Nếu MaNv là int: int id = int.Parse(maNv); ... x.MaNv == id
            // Nếu MaNv là string: ... x.MaNv == maNv
            var nhanVien = db.NhanViens.SingleOrDefault(x => x.MaNv.ToString() == maNv);

            if (nhanVien == null) return NotFound();

            return View(nhanVien);
        }
        [Authorize]
        public IActionResult LichSuDonHang(int? page)
        {
            // 1. Chặn Admin/Staff
            if (User.IsInRole("Admin") || User.IsInRole("Staff"))
            {
                return View("AdminNotification");
            }

            // 2. Lấy mã khách hàng
            var maKH = User.FindFirst("CustomerId")?.Value;
            if (maKH == null) return RedirectToAction("DangNhap", "KhachHang");

            // --- PHẦN PHÂN TRANG ---
            int pageSize = 15; // Ní muốn hiện bao nhiêu đơn 1 trang thì sửa số này
            int pageNumber = (page ?? 1); // Trang hiện tại, mặc định là 1

            var query = db.HoaDons
                          .Where(hd => hd.MaKh == maKH)
                          .OrderByDescending(hd => hd.NgayDat);

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Lấy dữ liệu theo trang
            var dsDonHang = query.Skip((pageNumber - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToList();

            // Đẩy dữ liệu phân trang ra View
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;

            return View(dsDonHang);
        }
        // Xem chi tiết một đơn hàng cụ thể
        [Authorize]
        public IActionResult ChiTietDonHang(int id)
        {
            var maKH = User.FindFirst("CustomerId")?.Value;
            if (maKH == null) return RedirectToAction("DangNhap");

            // Lấy đơn hàng kèm theo chi tiết và thông tin hàng hóa
            var donHang = db.HoaDons
                .Include(hd => hd.ChiTietHds)
                .ThenInclude(ct => ct.MaHhNavigation)
                .FirstOrDefault(hd => hd.MaHd == id);

            if (donHang == null)
            {
                return NotFound();
            }

            // KIỂM TRA BẢO MẬT: Đơn hàng này có phải của khách này không?
            // Nếu không phải (ví dụ cố tình gõ ID đơn người khác) -> Đá về trang danh sách
            if (donHang.MaKh != maKH)
            {
                return RedirectToAction("LichSuDonHang");
            }

            return View(donHang);
        }
        #endregion
        #region Quên Mật Khẩu

        // 1. Hiện trang nhập Email
        public IActionResult QuenMatKhau()
        {
            return View();
        }

        // 2. Xử lý gửi OTP
        [HttpPost]
        public async Task<IActionResult> QuenMatKhau(string email)
        {
            //var khachHang = db.KhachHangs.SingleOrDefault(kh => kh.Email == email);
            // Dùng SingleOrDefaultAsync và nhớ thêm await ở trước
            //var khachHang = await db.KhachHangs.SingleOrDefaultAsync(kh => kh.Email == email);
            var khachHang = await db.KhachHangs.SingleOrDefaultAsync(kh => kh.Email.Trim().ToLower() == email.Trim().ToLower()); // xóa khoản trắng tự động
            if (khachHang == null)
            {
                ModelState.AddModelError("Loi", "Email này chưa đăng ký tài khoản.");
                return View();
            }

            // Tạo mã OTP ngẫu nhiên 6 số
            string otp = new Random().Next(100000, 999999).ToString();

            // Lưu OTP vào cột RandomKey trong DB để lát nữa kiểm tra
            khachHang.RandomKey = otp;
            db.Update(khachHang);
            await db.SaveChangesAsync();

            // Gửi Email
            string subject = "Mã xác nhận Quên mật khẩu - MVCNASTORE";
            string content = $"Mã xác nhận của bạn là: <b style='color:red; font-size:20px;'>{otp}</b>";

            await _emailSender.SendEmailAsync(email, subject, content);

            // Chuyển sang trang nhập mã
            return RedirectToAction("KhoiPhucMatKhau", new { email = email });
        }

        // 3. Hiện trang nhập Mã OTP & Mật khẩu mới
        public IActionResult KhoiPhucMatKhau(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        // 4. Xử lý Đổi mật khẩu
        [HttpPost]
        public async Task<IActionResult> KhoiPhucMatKhau(string email, string otp, string matKhauMoi)
        {
            var khachHang = db.KhachHangs.SingleOrDefault(kh => kh.Email == email);
            if (khachHang == null) return NotFound();

            // Kiểm tra mã OTP
            if (khachHang.RandomKey != otp)
            {
                ModelState.AddModelError("Loi", "Mã xác nhận không đúng.");
                ViewBag.Email = email;
                return View();
            }

            // Reset lại RandomKey để mã cũ không dùng được nữa
            khachHang.RandomKey = Guid.NewGuid().ToString();

            // Mã đúng -> Đổi mật khẩu (Nhớ mã hóa)
            khachHang.MatKhau = _passwordHasher.HashPassword(khachHang, matKhauMoi);

            db.Update(khachHang);
            await db.SaveChangesAsync();

            TempData["Message"] = "Đổi mật khẩu thành công. Mời bạn đăng nhập.";
            return RedirectToAction("DangNhap");
        }
        #endregion
        [Authorize]
        public IActionResult EditProfile()
        {
            var maKh = User.FindFirst("CustomerId")?.Value;
            var kh = db.KhachHangs.SingleOrDefault(k => k.MaKh == maKh);
            if (kh == null) return NotFound();

            var model = new EditProfileVM
            {
                MaKh = kh.MaKh,
                HoTen = kh.HoTen,
                DienThoai = kh.DienThoai,
                Email = kh.Email,
                DiaChi = kh.DiaChi,
                GioiTinh = kh.GioiTinh,
                HinhCu = kh.Hinh
            };
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditProfile(EditProfileVM model)
        {
            if (ModelState.IsValid)
            {
                var kh = db.KhachHangs.SingleOrDefault(k => k.MaKh == model.MaKh);
                if (kh != null)
                {
                    kh.HoTen = model.HoTen;
                    kh.DienThoai = model.DienThoai;
                    kh.Email = model.Email;
                    kh.DiaChi = model.DiaChi;
                    kh.GioiTinh = model.GioiTinh;

                    // Xử lý Upload ảnh nếu có file mới
                    if (model.FileHinh != null)
                    {
                        // 1. Tạo tên file duy nhất
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.FileHinh.FileName);

                        // 2. Xác định đường dẫn tuyệt đối đến thư mục anhKH
                        string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "anhKH");
                        string filePath = Path.Combine(uploadDir, fileName);

                        // 3. [OPTIONAL] Xóa ảnh cũ nếu nó không phải là ảnh mặc định
                        if (!string.IsNullOrEmpty(kh.Hinh) && kh.Hinh != "default.jpg")
                        {
                            string oldFilePath = Path.Combine(uploadDir, kh.Hinh);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // 4. Lưu file mới vào thư mục
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.FileHinh.CopyToAsync(stream);
                        }

                        // 5. Cập nhật tên file mới vào Entity để lưu xuống DB
                        kh.Hinh = fileName;
                    }

                    db.Update(kh);
                    await db.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật hồ sơ thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    // Nếu chạy vào đây là do MaKh gửi lên không tìm thấy trong DB
                    TempData["Error"] = "Không tìm thấy thông tin khách hàng để cập nhật!";
                }
            }
            return View(model);
        }
        [Authorize]
        [HttpGet]
        public IActionResult DoiMatKhau() => View();

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DoiMatKhau(DoiMatKhauVM model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Lấy thông tin khách hàng hiện tại
            var maKh = User.FindFirst("CustomerId")?.Value;
            var kh = db.KhachHangs.SingleOrDefault(k => k.MaKh == maKh);

            if (kh == null) return RedirectToAction("DangNhap");

            // 2. Kiểm tra mật khẩu cũ có đúng không
            var result = _passwordHasher.VerifyHashedPassword(kh, kh.MatKhau, model.MatKhauCu);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Mật khẩu cũ không chính xác rồi ní ơi!");
                return View(model);
            }

            // 3. Nếu đúng thì băm mật khẩu mới và lưu lại
            kh.MatKhau = _passwordHasher.HashPassword(kh, model.MatKhauMoi);
            db.Update(kh);
            await db.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công rực rỡ!";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult TuChoi()
        {
            return View();
        }



    }
}