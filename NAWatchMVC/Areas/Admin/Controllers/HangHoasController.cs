using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;
using NAWatchMVC.Helpers;

namespace NAWatchMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")] // <--- CHỈ ADMIN VÀ STAFF MỚI ĐƯỢC VÀO
    public class HangHoasController : Controller
    {
        private readonly NawatchMvcContext _context;

        public HangHoasController(NawatchMvcContext context)
        {
            _context = context;
        }

        // GET: Admin/HangHoas
        public async Task<IActionResult> Index()
        {
            ViewBag.MaLoai = new SelectList(_context.Loais, "TenLoai", "TenLoai");
            ViewBag.MaNcc = new SelectList(_context.NhaCungCaps, "TenCongTy", "TenCongTy");
            var nawatchMvcContext = _context.HangHoas.Include(h => h.MaLoaiNavigation).Include(h => h.MaNccNavigation);
            return View(await nawatchMvcContext.ToListAsync());
        }

        // GET: Admin/HangHoas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hangHoa = await _context.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .Include(h => h.MaNccNavigation)
                .FirstOrDefaultAsync(m => m.MaHh == id);
            if (hangHoa == null)
            {
                return NotFound();
            }

            return View(hangHoa);
        }

        // GET: Admin/HangHoas/Create
        public IActionResult Create()
        {
            ViewData["MaLoai"] = new SelectList(_context.Loais, "MaLoai", "MaLoai");
            ViewData["MaNcc"] = new SelectList(_context.NhaCungCaps, "MaNcc", "MaNcc");
            return View();
        }

        // POST: Admin/HangHoas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("MaHh,TenHh,TenAlias,MaLoai,MaNcc,DonGia,GiamGia,Hinh,NgaySx,SoLanXem,MoTa,SoLuongBan,SoLuong,GioiTinh,DuongKinhMat,ChatLieuDay,DoRongDay,ChatLieuKhungVien,ChatLieuKinh,TenBoMay,ChongNuoc,TienIch,NguonNangLuong,LoaiMay,BoSuuTap,XuatXu,DiemDanhGia,ThoiGianPin")] HangHoa hangHoa)
        public async Task<IActionResult> Create([Bind("MaHh,TenHh,TenAlias,MaLoai,MaNcc,DonGia,GiamGia,MoTa,SoLuong,GioiTinh,DuongKinhMat,ChatLieuDay,DoRongDay,ChatLieuKhungVien,ChatLieuKinh,TenBoMay,ChongNuoc,TienIch,NguonNangLuong,LoaiMay,BoSuuTap,XuatXu,ThoiGianPin")] HangHoa hangHoa, IFormFile fHinh)
        {
            // 1. "MIỄN TỬ KIM BÀI": Bỏ qua kiểm tra các trường không có trên Form
            ModelState.Remove("MaLoaiNavigation");
            ModelState.Remove("MaNccNavigation");
            ModelState.Remove("Hinh"); // Tự xử lý lưu ảnh bên dưới

            // 2. TỰ ĐỘNG TẠO ALIAS: Nếu Admin để trống ô Alias, mình tự tạo từ Tên sản phẩm
            if (string.IsNullOrEmpty(hangHoa.TenAlias))
            {
                hangHoa.TenAlias = MyUtil.ToUrlSlug(hangHoa.TenHh);
            }

            // 3. GÁN GIÁ TRỊ MẶC ĐỊNH: Thỏa mãn điều kiện Database
            hangHoa.NgaySx = DateTime.Now;
            hangHoa.SoLanXem = 0;
            hangHoa.SoLuongBan = 0;
            hangHoa.DiemDanhGia = 0;

            if (ModelState.IsValid)
            {
                // 4. XỬ LÝ LƯU HÌNH ẢNH: Lưu vào thư mục wwwroot/anhall
                if (fHinh != null && fHinh.Length > 0)
                {
                    // Gọi hàm UploadHinh từ Helper ní vừa sửa
                    string fileName = MyUtil.UploadHinh(fHinh, "anhall");
                    hangHoa.Hinh = fileName;
                }
                else
                {
                    hangHoa.Hinh = "default.jpg"; // Hình mặc định nếu không chọn ảnh
                }

                try
                {
                    _context.Add(hangHoa);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Thêm đồng hồ mới thành công rồi ní ơi!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi lưu Database: " + ex.Message);
                }
            }

            // 5. NẾU LỖI: Load lại dữ liệu cho các ô Dropdown (Danh mục & Nhà cung cấp)
            // Ní nhớ kiểm tra đúng tên cột TenLoai/TenNcc trong DB của ní nhé
            ViewBag.MaLoai = new SelectList(_context.Loais, "MaLoai", "TenLoai", hangHoa.MaLoai);
            ViewBag.MaNcc = new SelectList(_context.NhaCungCaps, "MaNcc", "TenNcc", hangHoa.MaNcc);

            return View(hangHoa);
        }

        // GET: Admin/HangHoas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hangHoa = await _context.HangHoas.FindAsync(id);
            if (hangHoa == null)
            {
                return NotFound();
            }
            ViewData["MaLoai"] = new SelectList(_context.Loais, "MaLoai", "MaLoai", hangHoa.MaLoai);
            ViewData["MaNcc"] = new SelectList(_context.NhaCungCaps, "MaNcc", "MaNcc", hangHoa.MaNcc);
            return View(hangHoa);
        }

        // POST: Admin/HangHoas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("MaHh,TenHh,TenAlias,MaLoai,MaNcc,DonGia,GiamGia,Hinh,NgaySx,SoLanXem,MoTa,SoLuongBan,SoLuong,GioiTinh,DuongKinhMat,ChatLieuDay,DoRongDay,ChatLieuKhungVien,ChatLieuKinh,TenBoMay,ChongNuoc,TienIch,NguonNangLuong,LoaiMay,BoSuuTap,XuatXu,DiemDanhGia,ThoiGianPin")] HangHoa hangHoa)
        public async Task<IActionResult> Edit(int id, [Bind("MaHh,TenHh,TenAlias,MaLoai,MaNcc,DonGia,GiamGia,Hinh,NgaySx,SoLanXem,MoTa,SoLuongBan,SoLuong,GioiTinh,DuongKinhMat,ChatLieuDay,DoRongDay,ChatLieuKhungVien,ChatLieuKinh,TenBoMay,ChongNuoc,TienIch,NguonNangLuong,LoaiMay,BoSuuTap,XuatXu,DiemDanhGia,ThoiGianPin")] HangHoa hangHoa, IFormFile? fHinh)
        {
            if (id != hangHoa.MaHh) return NotFound();

            // 1. "MIỄN TỬ KIM BÀI": Loại bỏ kiểm tra các trường gây lỗi logic
            ModelState.Remove("MaLoaiNavigation");
            ModelState.Remove("MaNccNavigation");
            ModelState.Remove("Hinh"); // Để mình tự xử lý ảnh cũ/mới

            if (ModelState.IsValid)
            {
                try
                {
                    // 2. XỬ LÝ HÌNH ẢNH
                    if (fHinh != null) // Nếu Admin chọn ảnh mới
                    {
                        // Lưu ảnh mới vào thư mục 'anhall' và lấy tên file mới
                        hangHoa.Hinh = MyUtil.UploadHinh(fHinh, "anhall");
                    }
                    // Nếu fHinh == null, nó sẽ tự lấy cái tên hình cũ từ thẻ <input type="hidden" asp-for="Hinh" /> mà ní đã đặt ở View

                    _context.Update(hangHoa);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Cập nhật đồng hồ {hangHoa.TenHh} thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HangHoaExists(hangHoa.MaHh)) return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi lưu dữ liệu: " + ex.Message);
                }
            }

            // 3. NẾU LỖI (Load lại Dropdown để không bị trắng trang)
            ViewBag.MaLoai = new SelectList(_context.Loais, "MaLoai", "TenLoai", hangHoa.MaLoai);
            ViewBag.MaNcc = new SelectList(_context.NhaCungCaps, "MaNcc", "TenNcc", hangHoa.MaNcc);

            return View(hangHoa);
        }

        // GET: Admin/HangHoas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hangHoa = await _context.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .Include(h => h.MaNccNavigation)
                .FirstOrDefaultAsync(m => m.MaHh == id);
            if (hangHoa == null)
            {
                return NotFound();
            }

            return View(hangHoa);
        }

        // POST: Admin/HangHoas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 1. Tìm sản phẩm và lôi theo cả danh sách chi tiết hóa đơn (để check điều kiện)
            var hangHoa = await _context.HangHoas
                .Include(h => h.ChiTietHds) // Ní kiểm tra xem trong model HangHoa.cs tên nó là ChiTietHds hay ChiTietHoadons nhé
                .FirstOrDefaultAsync(m => m.MaHh == id);

            if (hangHoa == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm này để xóa ní ơi!";
                return RedirectToAction(nameof(Index));
            }

            // 2. CHECK ĐIỀU KIỆN: Đã có ai mua chiếc đồng hồ này chưa?
            if (hangHoa.ChiTietHds != null && hangHoa.ChiTietHds.Any())
            {
                // Nếu đã có đơn hàng, tuyệt đối không cho xóa vĩnh viễn (tránh lỗi Null ở báo cáo)
                TempData["Error"] = $"<b>Không thể xóa!</b> Sản phẩm <b>{hangHoa.TenHh}</b> đã tồn tại trong các đơn hàng cũ. Ní hãy dùng chức năng <b>Sửa</b> để chỉnh số lượng về 0 thay vì xóa nhé!";

                // Trả về lại trang Delete để Admin đọc thông báo lỗi
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            try
            {
                // 3. Nếu chưa bán cho ai, cho phép xóa vĩnh viễn khỏi Database
                _context.HangHoas.Remove(hangHoa);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã xóa sản phẩm <b>{hangHoa.TenHh}</b> thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống khi xóa: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool HangHoaExists(int id)
        {
            return _context.HangHoas.Any(e => e.MaHh == id);
        }
    }
}
