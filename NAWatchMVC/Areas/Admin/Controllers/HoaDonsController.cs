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
    public class HoaDonsController : Controller
    {
        private readonly NawatchMvcContext _context;

        public HoaDonsController(NawatchMvcContext context)
        {
            _context = context;
        }

        // GET: Admin/HoaDons
        public async Task<IActionResult> Index()
        {
            var nawatchMvcContext = _context.HoaDons.Include(h => h.MaKhNavigation).Include(h => h.MaNvNavigation).Include(h => h.MaTrangThaiNavigation).Include(h => h.ChiTietHds);
            return View(await nawatchMvcContext.ToListAsync());
        }

        // GET: Admin/HoaDons/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hoaDon = await _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaNvNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds) // Hoặc HoaDonChiTiets tùy model ní
                .ThenInclude(ct => ct.MaHhNavigation)
                    .FirstOrDefaultAsync(m => m.MaHd == id);
                
            if (hoaDon == null)
            {
                return NotFound();
            }

            return View(hoaDon);
        }

        // GET: Admin/HoaDons/Create
        public IActionResult Create()
        {
            ViewData["MaKh"] = new SelectList(_context.KhachHangs, "MaKh", "MaKh");
            ViewData["MaNv"] = new SelectList(_context.NhanViens, "MaNv", "MaNv");
            //ViewData["MaTrangThai"] = new SelectList(_context.TrangThais, "MaTrangThai", "MaTrangThai");
            ViewData["MaTrangThai"] = new SelectList(_context.TrangThais, "MaTrangThai", "TenTrangThai");
            return View();
        }

        // POST: Admin/HoaDons/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaHd,MaKh,NgayDat,NgayGiao,HoTen,DiaChi,DienThoai,CachThanhToan,CachVanChuyen,PhiVanChuyen,MaTrangThai,MaNv,GhiChu,TongTien")] HoaDon hoaDon)
        {
            // 1. "Xóa" mấy cái lỗi vô lý của thuộc tính điều hướng (Navigation) đi ní
            ModelState.Remove("MaKhNavigation");
            ModelState.Remove("MaNvNavigation");
            ModelState.Remove("MaTrangThaiNavigation");
            // Nếu ní có đơn vị vận chuyển thì thêm dòng này luôn cho chắc
            ModelState.Remove("MaVnNavigation");

            if (ModelState.IsValid)
            {
                // 2. Admin lên đơn giúp khách thì mặc định lấy giờ hiện tại cho chuẩn bài
                if (hoaDon.NgayDat == null)
                {
                    hoaDon.NgayDat = DateTime.Now;
                }

                _context.Add(hoaDon);
                await _context.SaveChangesAsync();

                // Tạo xong Hóa đơn (Phần khung) thì thường sẽ sang trang Index 
                // Hoặc ní có thể chuyển hướng sang trang thêm sản phẩm (ChiTietHoaDon)
                return RedirectToAction(nameof(Index));
            }

            // 3. Nếu lỡ có lỗi khác (ví dụ thiếu địa chỉ), hiện lại SelectList 
            // Tui sửa lại để nó hiện "Tên" thay vì mỗi cái "Mã ID" cho Admin dễ nhìn nhé
            ViewData["MaKh"] = new SelectList(_context.KhachHangs, "MaKh", "HoTen", hoaDon.MaKh);
            ViewData["MaNv"] = new SelectList(_context.NhanViens, "MaNv", "HoTen", hoaDon.MaNv);
            ViewData["MaTrangThai"] = new SelectList(_context.TrangThais, "MaTrangThai", "TenTrangThai", hoaDon.MaTrangThai);

            return View(hoaDon);
        }

        // GET: Admin/HoaDons/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hoaDon = await _context.HoaDons.FindAsync(id);
            if (hoaDon == null)
            {
                return NotFound();
            }
            ViewData["MaKh"] = new SelectList(_context.KhachHangs, "MaKh", "MaKh", hoaDon.MaKh);
            ViewData["MaNv"] = new SelectList(_context.NhanViens, "MaNv", "MaNv", hoaDon.MaNv);
            ViewData["MaTrangThai"] = new SelectList(_context.TrangThais, "MaTrangThai", "MaTrangThai", hoaDon.MaTrangThai);
            return View(hoaDon);
        }

        // POST: Admin/HoaDons/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaHd,MaKh,NgayDat,NgayGiao,HoTen,DiaChi,DienThoai,CachThanhToan,CachVanChuyen,PhiVanChuyen,MaTrangThai,MaNv,GhiChu,TongTien")] HoaDon hoaDon)
        {
            if (id != hoaDon.MaHd)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hoaDon);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HoaDonExists(hoaDon.MaHd))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaKh"] = new SelectList(_context.KhachHangs, "MaKh", "MaKh", hoaDon.MaKh);
            ViewData["MaNv"] = new SelectList(_context.NhanViens, "MaNv", "MaNv", hoaDon.MaNv);
            ViewData["MaTrangThai"] = new SelectList(_context.TrangThais, "MaTrangThai", "MaTrangThai", hoaDon.MaTrangThai);
            return View(hoaDon);
        }

        // GET: Admin/HoaDons/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hoaDon = await _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaNvNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .FirstOrDefaultAsync(m => m.MaHd == id);
            if (hoaDon == null)
            {
                return NotFound();
            }

            return View(hoaDon);
        }

        // POST: Admin/HoaDons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hoaDon = await _context.HoaDons.FindAsync(id);
            if (hoaDon != null)
            {
                _context.HoaDons.Remove(hoaDon);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HoaDonExists(int id)
        {
            return _context.HoaDons.Any(e => e.MaHd == id);
        }
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var hoaDon = await _context.HoaDons.FindAsync(id);
            if (hoaDon != null)
            {
                hoaDon.MaTrangThai = 1; // 1: Đang giao
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã duyệt đơn hàng thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public IActionResult GetKhachHangInfo(string id)
        {
            var kh = _context.KhachHangs.FirstOrDefault(k => k.MaKh == id);
            if (kh == null) return NotFound();

            // Trả về các thông tin cần thiết
            return Json(new
            {
                hoTen = kh.HoTen,
                dienThoai = kh.DienThoai,
                diaChi = kh.DiaChi
            });
        }
    }
}
