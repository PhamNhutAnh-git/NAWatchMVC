using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;

namespace NAWatchMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BoSuuTapHomesController : Controller
    {
        private readonly NawatchMvcContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public BoSuuTapHomesController(NawatchMvcContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Admin/BoSuuTapHomes
        public async Task<IActionResult> Index()
        {
            // Thêm OrderBy để ní dễ quản lý thứ tự hiển thị ngoài Home
            return View(await _context.BoSuuTapHomes.OrderBy(x => x.Order).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var bst = await _context.BoSuuTapHomes.FirstOrDefaultAsync(m => m.Id == id);
            if (bst == null) return NotFound();
            return View(bst);
        }

        // GET: Admin/BoSuuTapHomes/Create
        [HttpGet] // Thêm cái này cho rõ ràng nè ní
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách tên Bộ sưu tập từ bảng HangHoas
            var distinctCollections = await _context.HangHoas
                .Where(h => !string.IsNullOrEmpty(h.BoSuuTap))
                .Select(h => h.BoSuuTap)
                .Distinct()
                .ToListAsync();

            // Nạp vào ViewBag. Nếu danh sách trống thì nó vẫn chạy, không bị sập web
            ViewBag.CollectionList = new SelectList(distinctCollections);

            return View();
        }

        // POST: Admin/BoSuuTapHomes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CollectionName,Order,IsActive")] BoSuuTapHome boSuuTapHome, IFormFile? HinhAnh)
        {
            if (ModelState.IsValid)
            {
                // XỬ LÝ HÌNH ẢNH (Bây giờ là optional - không bắt buộc)
                if (HinhAnh != null && HinhAnh.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(HinhAnh.FileName);
                    string pathFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "collections");
                    if (!Directory.Exists(pathFolder)) Directory.CreateDirectory(pathFolder);

                    using (var stream = new FileStream(Path.Combine(pathFolder, fileName), FileMode.Create))
                    {
                        await HinhAnh.CopyToAsync(stream);
                    }
                    boSuuTapHome.BackgroundImage = fileName;
                }
                else
                {
                    // Nếu ní không nhập ảnh, tui gán cho nó 1 cái tên ảnh mặc định 
                    // Hoặc ní có thể để null nếu Database cho phép
                    boSuuTapHome.BackgroundImage = "no-image-collection.png";
                }

                _context.Add(boSuuTapHome);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nạp lại danh sách nếu lỗi
            await LoadCollectionList();
            return View(boSuuTapHome);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var bst = await _context.BoSuuTapHomes.FindAsync(id);
            if (bst == null) return NotFound();
            return View(bst);
        }

        // POST: Admin/BoSuuTapHomes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CollectionName,Order,IsActive")] BoSuuTapHome boSuuTapHome, IFormFile? HinhAnh)
        {
            if (id != boSuuTapHome.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBst = await _context.BoSuuTapHomes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

                    if (HinhAnh != null && HinhAnh.Length > 0)
                    {
                        // Có ảnh mới -> Xóa ảnh cũ, lưu ảnh mới
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(HinhAnh.FileName);
                        string pathFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "collections");

                        if (!string.IsNullOrEmpty(existingBst.BackgroundImage) && existingBst.BackgroundImage != "no-image-collection.png")
                        {
                            string oldPath = Path.Combine(pathFolder, existingBst.BackgroundImage);
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }

                        using (var stream = new FileStream(Path.Combine(pathFolder, fileName), FileMode.Create))
                        {
                            await HinhAnh.CopyToAsync(stream);
                        }
                        boSuuTapHome.BackgroundImage = fileName;
                    }
                    else
                    {
                        // KHÔNG CÓ ẢNH MỚI -> Giữ nguyên ảnh cũ từ Database
                        boSuuTapHome.BackgroundImage = existingBst.BackgroundImage;
                    }

                    _context.Update(boSuuTapHome);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BoSuuTapHomeExists(boSuuTapHome.Id)) return NotFound(); else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await LoadCollectionList();
            return View(boSuuTapHome);
        }

        // Hàm phụ để nạp lại danh sách cho gọn code
        private async Task LoadCollectionList()
        {
            var distinctCollections = await _context.HangHoas
                .Where(h => !string.IsNullOrEmpty(h.BoSuuTap))
                .Select(h => h.BoSuuTap).Distinct().ToListAsync();
            ViewBag.CollectionList = new SelectList(distinctCollections);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var bst = await _context.BoSuuTapHomes.FirstOrDefaultAsync(m => m.Id == id);
            if (bst == null) return NotFound();
            return View(bst);
        }
        // GET: Admin/BoSuuTapHomes/Delete/5
        // Hàm này để hiện cái trang xác nhận mà ní vừa gửi code cho tui đó
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null) return NotFound();

        //    var boSuuTapHome = await _context.BoSuuTapHomes
        //        .FirstOrDefaultAsync(m => m.Id == id);

        //    if (boSuuTapHome == null) return NotFound();

        //    return View(boSuuTapHome);
        //}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var boSuuTapHome = await _context.BoSuuTapHomes.FindAsync(id);

            if (boSuuTapHome != null)
            {
                // 1. XỬ LÝ XÓA FILE ẢNH VẬT LÝ (Để dọn rác server)
                if (!string.IsNullOrEmpty(boSuuTapHome.BackgroundImage))
                {
                    // Trỏ đúng vào thư mục images/collections mà ní dùng ở View
                    string pathFile = Path.Combine(_hostEnvironment.WebRootPath, "images", "collections", boSuuTapHome.BackgroundImage);

                    if (System.IO.File.Exists(pathFile))
                    {
                        System.IO.File.Delete(pathFile); // "Trảm" luôn file ảnh
                    }
                }

                // 2. XÓA TRONG DATABASE
                _context.BoSuuTapHomes.Remove(boSuuTapHome);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BoSuuTapHomeExists(int id) => _context.BoSuuTapHomes.Any(e => e.Id == id);
    }
}