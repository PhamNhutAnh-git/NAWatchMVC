using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;


namespace NAWatchMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    // Nếu muốn cả Admin và Staff vào xem danh sách, dùng: [Authorize(Roles = "Admin,Staff")]
    // Nhưng thường trang Quản lý Nhân viên chỉ dành cho Admin (Roles = "Admin")
    [Authorize(Roles = "Admin")]
    public class NhanViensController : Controller
    {
        private readonly NawatchMvcContext _context;
        // Thêm dòng này để gọi thợ băm của Microsoft
        private readonly IPasswordHasher<NhanVien> _passwordHasher;
        public NhanViensController(NawatchMvcContext context, IPasswordHasher<NhanVien> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher; // Gán vào để xài
        }

        // GET: Admin/NhanViens
        public async Task<IActionResult> Index()
        {
            // Lấy toàn bộ danh sách để Admin quản lý (kể cả người đã bị vô hiệu hóa)
            return View(await _context.NhanViens.ToListAsync());
        }

        // GET: Admin/NhanViens/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(m => m.MaNv == id);
            if (nhanVien == null) return NotFound();

            return View(nhanVien);
        }

        // GET: Admin/NhanViens/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/NhanViens/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNv,HoTen,Email,MatKhau")] NhanVien nhanVien)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra trùng MaNv hoặc Email trước khi tạo
                if (_context.NhanViens.Any(n => n.MaNv == nhanVien.MaNv || n.Email == nhanVien.Email))
                {
                    ModelState.AddModelError("", "Mã nhân viên hoặc Email đã tồn tại!");
                    return View(nhanVien);
                }
                else
                {
                    nhanVien.VaiTro = 2; // Staff
                    nhanVien.HieuLuc = true;

                    // DÙNG ĐÚNG CÔNG THỨC NÀY:
                    nhanVien.MatKhau = _passwordHasher.HashPassword(nhanVien, nhanVien.MatKhau);

                    _context.Add(nhanVien);
                    await _context.SaveChangesAsync();
                }
                TempData["Success"] = "Tạo nhân viên mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(nhanVien);
        }

        // GET: Admin/NhanViens/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null) return NotFound();

            return View(nhanVien);
        }

        // POST: Admin/NhanViens/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaNv,HoTen,Email,MatKhau,VaiTro,HieuLuc")] NhanVien nhanVien)
        {
            if (id != nhanVien.MaNv) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Lấy dữ liệu cũ từ Database để so sánh (Dùng AsNoTracking để tránh xung đột)
                    var existingNv = await _context.NhanViens.AsNoTracking().FirstOrDefaultAsync(x => x.MaNv == id);
                    if (existingNv == null) return NotFound();

                    // 2. KIỂM TRA ĐỔI MẬT KHẨU:
                    // Nếu ô mật khẩu trên Form trống HOẶC giống hệt chuỗi Hash cũ -> Không đổi mật khẩu
                    if (string.IsNullOrEmpty(nhanVien.MatKhau) || nhanVien.MatKhau == existingNv.MatKhau)
                    {
                        nhanVien.MatKhau = existingNv.MatKhau; // Giữ lại Hash cũ
                    }
                    else
                    {
                        // Nếu người dùng nhập chữ mới (vd: "123") -> Tiến hành băm mới
                        // Dùng _passwordHasher đã tiêm vào Constructor
                        nhanVien.MatKhau = _passwordHasher.HashPassword(nhanVien, nhanVien.MatKhau);
                    }

                    _context.Update(nhanVien);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật nhân viên thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NhanVienExists(nhanVien.MaNv)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(nhanVien);
        }
        // 1. HÀM GET: Dùng để MỞ trang xác nhận (Cái trang Delete.cshtml xịn sò nãy mình độ đó)
        // Khi ní bấm nút "Vô hiệu hóa" ở trang Index, nó sẽ chạy vào đây trước.
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(m => m.MaNv == id);

            if (nhanVien == null) return NotFound();

            return View(nhanVien); // Nó sẽ tìm đến file Views/NhanViens/Delete.cshtml
        }
        // POST: Admin/NhanViens/Delete/5 (Soft Delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien != null)
            {
                // Soft Delete: Không xóa khỏi DB, chỉ tắt hiệu lực
                nhanVien.HieuLuc = false;
                _context.Update(nhanVien);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã vô hiệu hóa nhân viên {nhanVien.HoTen}!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool NhanVienExists(string id)
        {
            return _context.NhanViens.Any(e => e.MaNv == id);
        }
    }
}