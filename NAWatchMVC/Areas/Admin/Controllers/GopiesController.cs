using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;

namespace NAWatchMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class GopiesController : Controller
    {
        private readonly NawatchMvcContext _context;

        public GopiesController(NawatchMvcContext context)
        {
            _context = context;
        }

        // GET: Admin/Gopies
        public async Task<IActionResult> Index()
        {
            return View(await _context.Gopies.ToListAsync());
        }

        // GET: Admin/Gopies/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gopY = await _context.Gopies
                .FirstOrDefaultAsync(m => m.MaGy == id);
            if (gopY == null)
            {
                return NotFound();
            }

            return View(gopY);
        }

        // GET: Admin/Gopies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Gopies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaGy,HoTen,Email,DienThoai,NoiDung,NgayGy,IsRead,IsVisible")] GopY gopY)
        {
            if (ModelState.IsValid)
            {
                _context.Add(gopY);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(gopY);
        }

        // GET: Admin/Gopies/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gopY = await _context.Gopies.FindAsync(id);
            if (gopY == null)
            {
                return NotFound();
            }
            return View(gopY);
        }

        // POST: Admin/Gopies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaGy,HoTen,Email,DienThoai,NoiDung,NgayGy,IsRead,IsVisible")] GopY gopY)
        {
            if (id != gopY.MaGy)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gopY);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GopYExists(gopY.MaGy))
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
            return View(gopY);
        }

        // GET: Admin/Gopies/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gopY = await _context.Gopies
                .FirstOrDefaultAsync(m => m.MaGy == id);
            if (gopY == null)
            {
                return NotFound();
            }

            return View(gopY);
        }

        // POST: Admin/Gopies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var gopY = await _context.Gopies.FindAsync(id);
            if (gopY != null)
            {
                _context.Gopies.Remove(gopY);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GopYExists(string id)
        {
            return _context.Gopies.Any(e => e.MaGy == id);
        }
        // --- LOGIC XỬ LÝ TRẠNG THÁI (STATE MACHINE) ---

        // 1. AJAX: Đổi trạng thái ĐANG XỬ LÝ (IsRead)
        [HttpPost]
        public async Task<IActionResult> ToggleProcessing(string id)
        {
            var gopY = await _context.Gopies.FindAsync(id);

            // LUẬT: Nếu đã Hoàn tất (IsVisible) thì không cho phép đổi trạng thái Đang làm
            if (gopY == null || gopY.IsVisible)
            {
                return Json(new { success = false, message = "Ticket đã đóng, không thể đổi trạng thái xử lý!" });
            }

            gopY.IsRead = !(gopY.IsRead ?? false);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isProcessing = gopY.IsRead,
                message = gopY.IsRead == true ? "Đã chuyển sang: Đang xử lý ⏳" : "Đã đưa về: Chờ xử lý"
            });
        }

        // 2. AJAX: Đổi trạng thái HOÀN TẤT (IsVisible)
        [HttpPost]
        public async Task<IActionResult> ToggleFinished(string id)
        {
            var gopY = await _context.Gopies.FindAsync(id);
            if (gopY == null) return Json(new { success = false });

            gopY.IsVisible = !gopY.IsVisible;

            // LUẬT CỦA NÍ: Nếu bật HOÀN TẤT thì phải TẮT Đang xử lý
            if (gopY.IsVisible)
            {
                gopY.IsRead = false;
            }

             _context.Update(gopY); // Đảm bảo update đúng thực thể
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isFinished = gopY.IsVisible,
                message = gopY.IsVisible ? "Đã giải quyết xong yêu cầu! ✅" : "Đã mở lại yêu cầu hỗ trợ"
            });
        }
    }
}
