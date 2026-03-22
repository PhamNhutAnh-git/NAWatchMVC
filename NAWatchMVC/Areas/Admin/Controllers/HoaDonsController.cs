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

            // Ní dùng FindAsync cũng được, nhưng tui khuyên nên dùng Include ChiTietHds 
            // để hiện danh sách sản phẩm có hình luôn như hồi nãy anh em mình bàn.
            var hoaDon = await _context.HoaDons
                .Include(h => h.ChiTietHds)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .FirstOrDefaultAsync(m => m.MaHd == id);

            if (hoaDon == null)
            {
                return NotFound();
            }

            // GỌI HÀM PHỤ Ở ĐÂY LÀ XONG, KHÔNG CẦN VIẾT LẠI 3 DÒNG VIEWDATA NỮA
            PrepareSelectList(hoaDon);

            return View(hoaDon);
        }

        // POST: Admin/HoaDons/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 1. THÊM "DaThanhToan" vào danh sách Bind để nhận dữ liệu từ checkbox Admin
        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("MaHd,MaKh,NgayDat,NgayGiao,HoTen,DiaChi,DienThoai,CachThanhToan,CachVanChuyen,PhiVanChuyen,MaTrangThai,MaNv,GhiChu,TongTien,DaThanhToan")] HoaDon hoaDon)
        {
            if (id != hoaDon.MaHd) return Json(new { success = false, message = "Lỗi dữ liệu!" });

            if (ModelState.IsValid)
            {
                try
                {
                    //var hoaDonCu = await _context.HoaDons.AsNoTracking().FirstOrDefaultAsync(h => h.MaHd == id);
                    var hoaDonCu = await _context.HoaDons.FirstOrDefaultAsync(h => h.MaHd == id);
                    if (hoaDonCu == null) return Json(new { success = false, message = "Đơn không tồn tại!" });
                    int oldStatus = hoaDonCu.MaTrangThai;
                    int newStatus = hoaDon.MaTrangThai;

                    // --- KIỂM TRA DATHANHTOAN TRƯỚC KHI LƯU ---
                    // 0. CHỐT CHẶN CỨNG: Nếu đơn cũ đã là 4 (Hủy) hoặc 5 (Hoàn tiền)
                    if (hoaDonCu.MaTrangThai == 4 || hoaDonCu.MaTrangThai == 5)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Ní ơi! Đơn này đã đóng (Hủy/Hoàn tiền) rồi, hệ thống đã khóa vĩnh viễn để bảo vệ dữ liệu, không sửa được đâu!"
                        });
                    }
                    // 1. 0. CHỐT CHẶN CỨNG: Nếu đơn cũ đã là 4 (Hủy) hoặc 5 (Hoàn tiền)
                    if (oldStatus == 4 || oldStatus == 5)
                        return Json(new { success = false, message = "Bạn ơi! Đơn này đã đóng (Hủy/Hoàn tiền) rồi, hệ thống đã khóa vĩnh viễn để bảo vệ dữ liệu, không sửa được đâu!!" });
                    // 0.5. CHẶN 2 QUAY VỀ 0, 1 (VÁ LỖ HỔNG)
                    if (oldStatus == 2 && (newStatus == 0 || newStatus == 1))
                        return Json(new { success = false, message = "Hàng đang đi giao rồi, không được lùi về trạng thái cũ!" });
                    // 1. Nếu là đơn VNPay (Mã 1) thì mặc định DaThanhToan phải luôn là TRUE
                    // (Trừ khi ní chuyển sang Hoàn tiền - Mã 5)
                    if (newStatus == 1)
                    {
                        hoaDon.DaThanhToan = true;
                    }

                    // 2. Chuyển sang HOÀN TẤT (Mã 3): Bắt buộc DaThanhToan phải tích chọn là TRUE
                    if (newStatus == 3)
                    {
                        if (oldStatus != 2) return Json(new { success = false, message = "Phải qua Đang giao (2) mới được Hoàn tất!" });

                        // ĐÂY LÀ CHỖ DÙNG DATHANHTOAN NÈ NÍ:
                        if (!hoaDon.DaThanhToan)
                        {
                            return Json(new { success = false, message = "Chặn: Ní chưa tích chọn 'Đã thanh toán' thì không được chuyển đơn sang Hoàn tất!" });
                        }
                    }
                    // 3. Chuyển sang HOÀN TIỀN (Mã 5): Tiền trả lại khách rồi nên DaThanhToan phải về FALSE
                    if (newStatus == 5)
                    {
                        if (oldStatus != 1 && oldStatus != 3)
                        {
                            return Json(new { success = false, message = "Chỉ hoàn tiền cho đơn đã thu tiền cho đã thanh toán và vnpay (Mã 1 hoặc 3)!" });
                        }
                        await RestoreStock(id); // Cộng lại kho

                        // ĐÂY LÀ CHỖ DÙNG DATHANHTOAN NÈ NÍ:
                        hoaDon.DaThanhToan = false;
                        hoaDon.GhiChu += " | [Hệ thống] Đã hoàn tiền, set trạng thái thanh toán về False.";
                    }
                    //3.5 CHẶN 3 CHỈ ĐƯỢC ĐI TIẾP SANG 5Nếu đơn cũ đã là 3, mà Admin chọn mã mới khác 5 (ví dụ chọn 4) -> Báo lỗi ngay
                    if (oldStatus == 3 && newStatus != 5 && newStatus != 3)
                    {
                        return Json(new { success = false, message = "Đơn Hoàn tất chỉ có thể chuyển duy nhất sang Hoàn tiền thôi ní ơi!" });
                    }
                    // 4. Chuyển sang HỦY (Mã 4): Thường là COD chưa thu tiền nên mặc định là FALSE
                    if (newStatus == 4)
                    {
                        if (oldStatus == 1) return Json(new { success = false, message = "Đơn VNPay không được Hủy, phải chọn Hoàn tiền!" });
                        if (oldStatus == 3) return Json(new { success = false, message = "Đơn hàng đã hoàn tất, phải chọn Hoàn tiền!" });
                        await RestoreStock(id);
                        hoaDon.DaThanhToan = false;
                    }

                    // --- 4. GÁN ĐÈ DỮ LIỆU (CHỈ GÁN NHỮNG THỨ ĐƯỢC SỬA) ---
                    hoaDonCu.MaTrangThai = newStatus;
                    hoaDonCu.HoTen = hoaDon.HoTen;
                    hoaDonCu.DiaChi = hoaDon.DiaChi;
                    hoaDonCu.DienThoai = hoaDon.DienThoai;
                    hoaDonCu.GhiChu = hoaDon.GhiChu;
                    hoaDonCu.DaThanhToan = hoaDon.DaThanhToan;
                    hoaDonCu.MaNv = hoaDon.MaNv;
                    //_context.Update(hoaDon);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Lưu thành công!" });
                }
                catch (Exception ex)
                {
                    var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return Json(new { success = false, message = "Lỗi SQL: " + message });
                    //return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
        }
        // Hàm phụ để code nhìn cho gọn (Ní viết thêm ở dưới cùng Controller)
        private void PrepareSelectList(HoaDon hoaDon)
        {
            ViewData["MaKh"] = new SelectList(_context.KhachHangs, "MaKh", "MaKh", hoaDon.MaKh);
            ViewData["MaNv"] = new SelectList(_context.NhanViens, "MaNv", "MaNv", hoaDon.MaNv);
            ViewData["MaTrangThai"] = new SelectList(_context.TrangThais, "MaTrangThai", "TenTrangThai", hoaDon.MaTrangThai);
        }
        // Hàm này phải là async vì nó có tương tác với Database nhiều lần
        private async Task RestoreStock(int maHd)
        {
            var details = await _context.ChiTietHds.Where(ct => ct.MaHd == maHd).ToListAsync();
            foreach (var item in details)
            {
                // Phải dùng FindAsync để EF nó lôi đúng đối tượng đang quản lý ra
                var sp = await _context.HangHoas.FindAsync(item.MaHh);
                if (sp != null)
                {
                    // 1. Cộng lại kho
                    sp.SoLuong = (sp.SoLuong ?? 0) + item.SoLuong;

                    // 2. Trừ số lượng bán (Phải cực kỳ cẩn thận chỗ này)
                    if (sp.SoLuongBan >= item.SoLuong)
                    {
                        sp.SoLuongBan -= item.SoLuong;
                    }
                    else
                    {
                        sp.SoLuongBan = 0;
                    }

                    // Đánh dấu là đối tượng này đã thay đổi
                    _context.Entry(sp).State = EntityState.Modified;
                }
            }
        }
        //private async Task RestoreStock(int maHd)
        //{
        //    // 1. Tìm tất cả các món hàng nằm trong cái hóa đơn này
        //    var chiTiets = await _context.ChiTietHds
        //                             .Where(ct => ct.MaHd == maHd)
        //                             .ToListAsync();

        //    // 2. Chạy vòng lặp qua từng món hàng để trả lại kho
        //    foreach (var item in chiTiets)
        //    {
        //        var sp = await _context.HangHoas.FindAsync(item.MaHh);
        //        if (sp != null)
        //        {
        //            // Tăng số lượng trong kho lên (Vì khách không mua nữa)
        //            sp.SoLuong += item.SoLuong;

        //            // Giảm số lượng đã bán đi (Để báo cáo bán hàng không bị sai)
        //            sp.SoLuongBan = (sp.SoLuongBan ?? 0) - item.SoLuong;

        //            _context.Update(sp); // Cập nhật lại món hàng
        //        }
        //    }
        //    // Lưu ý: Không cần SaveChangesAsync ở đây vì nó sẽ được Save chung ở hàm Edit chính
        //}

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
