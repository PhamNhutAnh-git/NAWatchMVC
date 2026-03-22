using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;

namespace NAWatchMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")] // <--- CHỈ ADMIN VÀ STAFF MỚI ĐƯỢC VÀO
    public class NhaCungCapsController : Controller
    {
        private readonly NawatchMvcContext _context;

        public NhaCungCapsController(NawatchMvcContext context)
        {
            _context = context;
        }

        // GET: Admin/NhaCungCaps
        public async Task<IActionResult> Index()
        {
            return View(await _context.NhaCungCaps.ToListAsync());
        }

        // GET: Admin/NhaCungCaps/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhaCungCap = await _context.NhaCungCaps
                .FirstOrDefaultAsync(m => m.MaNcc == id);
            if (nhaCungCap == null)
            {
                return NotFound();
            }

            return View(nhaCungCap);
        }

        // GET: Admin/NhaCungCaps/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/NhaCungCaps/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNcc,TenCongTy,NguoiLienLac,Email,DienThoai,DiaChi,MoTa,QuocGia")] NhaCungCap nhaCungCap, IFormFile Hinh)
        {
            // BỎ QUA kiểm tra lỗi cho 2 thằng này
            ModelState.Remove("Logo"); // Vì trong Model nó bắt buộc nhưng Form không có ô nhập Logo
            ModelState.Remove("Hinh"); // Vì đây là tham số phụ, không phải cột trong DB

            if (ModelState.IsValid)
            {
                // 1. Xử lý lưu file ảnh (nếu ní có chọn ảnh)
                if (Hinh != null && Hinh.Length > 0)
                {
                    // Tạo tên file ngẫu nhiên để không trùng
                    string fileName = Guid.NewGuid().ToString().Substring(0, 8) + "_" + Hinh.FileName;
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "anhncc", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await Hinh.CopyToAsync(stream);
                    }

                    // Gán cái tên file vừa tạo vào cột Logo để lưu xuống DB
                    nhaCungCap.Logo = fileName;
                }
                else
                {
                    // Nếu không chọn ảnh thì cho nó cái ảnh mặc định
                    nhaCungCap.Logo = "ncckhac.png";
                }

                // 2. Lưu xuống Database
                _context.Add(nhaCungCap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu vẫn lỗi thì trả về View để xem báo lỗi gì tiếp
            return View(nhaCungCap);
        }

        // GET: Admin/NhaCungCaps/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhaCungCap = await _context.NhaCungCaps.FindAsync(id);
            if (nhaCungCap == null)
            {
                return NotFound();
            }
            return View(nhaCungCap);
        }

        // POST: Admin/NhaCungCaps/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaNcc,TenCongTy,Logo,NguoiLienLac,Email,DienThoai,DiaChi,MoTa,QuocGia")] NhaCungCap nhaCungCap, IFormFile fLogo)
        {
            if (id != nhaCungCap.MaNcc)
            {
                return NotFound();
            }

            // Bỏ qua kiểm tra lỗi cho Logo và fLogo
            ModelState.Remove("Logo");
            ModelState.Remove("fLogo");

            if (ModelState.IsValid)
            {
                try
                {
                    if (fLogo != null && fLogo.Length > 0)
                    {
                        // Tình huống 1: Admin CHỌN ẢNH MỚI
                        // Tạo tên file mới (không dùng Guid quá dài để tránh lỗi truncated cũ)
                        string fileName = DateTime.Now.Ticks.ToString() + Path.GetExtension(fLogo.FileName);
                        string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "anhncc", fileName);

                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await fLogo.CopyToAsync(stream);
                        }

                        // Cập nhật tên file mới vào Logo
                        nhaCungCap.Logo = fileName;
                    }
                    // Tình huống 2: Admin KHÔNG chọn ảnh mới
                    // nhaCungCap.Logo sẽ giữ nguyên giá trị cũ (lấy từ thẻ hidden trong View gửi lên)

                    _context.Update(nhaCungCap);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NhaCungCapExists(nhaCungCap.MaNcc))
                    {
                        return NotFound();
                    }
                    else { throw; }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(nhaCungCap);
        }

        // GET: Admin/NhaCungCaps/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var nhaCungCap = await _context.NhaCungCaps
                .FirstOrDefaultAsync(m => m.MaNcc == id);
            if (nhaCungCap == null)
            {
                return NotFound();
            }

            return View(nhaCungCap);
        }

        // POST: Admin/NhaCungCaps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var nhaCungCap = await _context.NhaCungCaps.FindAsync(id);
            if (nhaCungCap != null)
            {
                _context.NhaCungCaps.Remove(nhaCungCap);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NhaCungCapExists(string id)
        {
            return _context.NhaCungCaps.Any(e => e.MaNcc == id);
        }
    }
}
