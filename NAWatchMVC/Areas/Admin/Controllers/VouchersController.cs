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
    public class VouchersController : Controller
    {
        private readonly NawatchMvcContext _context;

        public VouchersController(NawatchMvcContext context)
        {
            _context = context;
        }

        // GET: Admin/Vouchers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Vouchers.ToListAsync());
        }

        // GET: Admin/Vouchers/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(m => m.MaVoucher == id);
            if (voucher == null)
            {
                return NotFound();
            }

            return View(voucher);
        }

        // GET: Admin/Vouchers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Vouchers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaVoucher,LoaiVoucher,LoaiGiamGia,GiaTriGiam,GiaTriDonHangToiThieu,GiamToiDa,NgayBatDau,NgayKetThuc,SoLuongToiDa,SoLuongDaDung,TrangThai")] Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                // --- BƯỚC QUAN TRỌNG: Kiểm tra trùng mã ---
                var exists = await _context.Vouchers.AnyAsync(v => v.MaVoucher == voucher.MaVoucher);
                if (exists)
                {
                    // Thêm lỗi vào ModelState để hiện ra ngoài View
                    ModelState.AddModelError("MaVoucher", "Mã voucher này đã tồn tại rồi ní ơi, nhập mã khác đi!");
                    return View(voucher);
                }

                try
                {
                    _context.Add(voucher);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu: " + ex.Message);
                }
            }
            return View(voucher);
        }

        // GET: Admin/Vouchers/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }
            return View(voucher);
        }

        // POST: Admin/Vouchers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("MaVoucher,LoaiVoucher,LoaiGiamGia,GiaTriGiam,GiaTriDonHangToiThieu,GiamToiDa,NgayBatDau,NgayKetThuc,SoLuongToiDa,SoLuongDaDung,TrangThai")] Voucher voucher)
        {
            if (id != voucher.MaVoucher)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(voucher);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VoucherExists(voucher.MaVoucher))
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
            return View(voucher);
        }

        // GET: Admin/Vouchers/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(m => m.MaVoucher == id);
            if (voucher == null)
            {
                return NotFound();
            }

            return View(voucher);
        }

        // POST: Admin/Vouchers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);

            if (voucher == null)
            {
                return NotFound();
            }

            // --- LOGIC BẢO VỆ DỮ LIỆU CỦA NÍ ---
            // Nếu mã đã phát sinh giao dịch (SoLuongDaDung > 0)
            if (voucher.SoLuongDaDung > 0)
            {
                // Gửi thông báo lỗi về trang Index
                TempData["MessageError"] = $"Không thể xóa mã [{id}] vì đã có {voucher.SoLuongDaDung} khách hàng sử dụng. Ní hãy chuyển trạng thái sang TẮT (OFF) để ngừng áp dụng nhé!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu chưa ai dùng thì xóa thoải mái
            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            TempData["MessageSuccess"] = $"Đã xóa thành công mã giảm giá [{id}]!";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return Json(new { success = false, message = "Không tìm thấy mã!" });
            }

            // Đảo ngược trạng thái: true -> false, false -> true
            voucher.TrangThai = !voucher.TrangThai;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                newState = voucher.TrangThai,
                message = voucher.TrangThai ? "Đã kích hoạt mã!" : "Đã tạm ngưng mã!"
            });
        }
        private bool VoucherExists(string id)
        {
            return _context.Vouchers.Any(e => e.MaVoucher == id);
        }
    }
}
