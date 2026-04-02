using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NAWatchMVC.Data;

namespace NAWatchMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class HomePageMediasController : Controller
    {
        private readonly NawatchMvcContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public HomePageMediasController(NawatchMvcContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Admin/HomePageMedias
        public async Task<IActionResult> Index()
        {
            return View(await _context.HomePageMedias.ToListAsync());
        }

        // GET: Admin/HomePageMedias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var homePageMedia = await _context.HomePageMedias.FirstOrDefaultAsync(m => m.Id == id);
            if (homePageMedia == null) return NotFound();

            return View(homePageMedia);
        }

        // GET: Admin/HomePageMedias/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Quan trọng: Thêm 'ViTri' vào Bind và dùng IFormFile? (nullable)
        public async Task<IActionResult> Create([Bind("Id,Title,YoutubeUrl,MediaType,NavigationLink,Order,IsActive,ViTri")] HomePageMedia homePageMedia, IFormFile? HinhAnh)
        {
            // 1. Kiểm tra logic bắt buộc ảnh: Nếu KHÔNG PHẢI Video thì BẮT BUỘC có ảnh
            if (homePageMedia.MediaType != "Video" && (HinhAnh == null || HinhAnh.Length == 0))
            {
                ModelState.AddModelError("HinhAnh", "Ní ơi, Slide hoặc Banner thì phải chọn hình ảnh mới đẹp!");
            }

            if (ModelState.IsValid)
            {
                // 2. Xử lý lưu Hình ảnh (Nếu có)
                if (HinhAnh != null && HinhAnh.Length > 0)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(HinhAnh.FileName);
                    string pathFolder = Path.Combine(wwwRootPath, "images", "homepage");

                    if (!Directory.Exists(pathFolder)) Directory.CreateDirectory(pathFolder);

                    string fullPath = Path.Combine(pathFolder, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await HinhAnh.CopyToAsync(stream);
                    }
                    homePageMedia.ImageUrl = fileName;
                }

                // 3. Xử lý chuẩn hóa link YouTube (Nhẫn thuật biến hình link)
                if (homePageMedia.MediaType == "Video" && !string.IsNullOrEmpty(homePageMedia.YoutubeUrl))
                {
                    if (homePageMedia.YoutubeUrl.Contains("watch?v="))
                    {
                        homePageMedia.YoutubeUrl = homePageMedia.YoutubeUrl.Replace("watch?v=", "embed/");
                    }
                    else if (homePageMedia.YoutubeUrl.Contains("youtu.be/"))
                    {
                        homePageMedia.YoutubeUrl = homePageMedia.YoutubeUrl.Replace("youtu.be/", "www.youtube.com/embed/");
                    }

                    // Cắt bỏ các tham số rác sau dấu &
                    if (homePageMedia.YoutubeUrl.Contains("&"))
                    {
                        homePageMedia.YoutubeUrl = homePageMedia.YoutubeUrl.Split('&')[0];
                    }
                }

                _context.Add(homePageMedia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(homePageMedia);
        }

        // GET: Admin/HomePageMedias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var homePageMedia = await _context.HomePageMedias.FindAsync(id);
            if (homePageMedia == null) return NotFound();

            return View(homePageMedia);
        }

        // POST: Admin/HomePageMedias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,YoutubeUrl,MediaType,NavigationLink,Order,IsActive,ViTri")] HomePageMedia homePageMedia, IFormFile? HinhAnh)
        {
            if (id != homePageMedia.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Lấy dữ liệu cũ để quản lý file ảnh
                    var existingMedia = await _context.HomePageMedias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (existingMedia == null) return NotFound();

                    // 2. Xử lý Hình ảnh
                    if (HinhAnh != null && HinhAnh.Length > 0)
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(HinhAnh.FileName);
                        string pathFolder = Path.Combine(wwwRootPath, "images", "homepage");

                        // Xóa file ảnh cũ trên server để dọn rác
                        if (!string.IsNullOrEmpty(existingMedia.ImageUrl))
                        {
                            string oldPath = Path.Combine(pathFolder, existingMedia.ImageUrl);
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        // Lưu file ảnh mới
                        string fullPath = Path.Combine(pathFolder, fileName);
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await HinhAnh.CopyToAsync(stream);
                        }
                        homePageMedia.ImageUrl = fileName;
                    }
                    else
                    {
                        // Nếu không up ảnh mới -> Giữ lại tên file ảnh cũ
                        homePageMedia.ImageUrl = existingMedia.ImageUrl;
                    }

                    // 3. Chuẩn hóa link YouTube (Nếu là loại Video)
                    if (homePageMedia.MediaType == "Video" && !string.IsNullOrEmpty(homePageMedia.YoutubeUrl))
                    {
                        if (homePageMedia.YoutubeUrl.Contains("watch?v="))
                        {
                            homePageMedia.YoutubeUrl = homePageMedia.YoutubeUrl.Replace("watch?v=", "embed/");
                        }
                        else if (homePageMedia.YoutubeUrl.Contains("youtu.be/"))
                        {
                            homePageMedia.YoutubeUrl = homePageMedia.YoutubeUrl.Replace("youtu.be/", "www.youtube.com/embed/");
                        }

                        if (homePageMedia.YoutubeUrl.Contains("&"))
                        {
                            homePageMedia.YoutubeUrl = homePageMedia.YoutubeUrl.Split('&')[0];
                        }
                    }

                    _context.Update(homePageMedia);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HomePageMediaExists(homePageMedia.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(homePageMedia);
        }
        // 1. Hàm GET: Để hiện cái trang Đỏ đỏ xác nhận xóa (Ní đang bị lỗi ở đây)
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var media = await _context.HomePageMedias.FirstOrDefaultAsync(m => m.Id == id);
            if (media == null) return NotFound();

            return View(media); // Nó sẽ tìm file Views/HomePageMedias/Delete.cshtml
        }
        // 2. Hàm POST: Xử lý xóa (PHẢI THÊM 2 DÒNG DƯỚI ĐÂY)
        [HttpPost, ActionName("Delete")] // <-- "Bùa chú" 1: Định danh đây là hành động Delete
        [ValidateAntiForgeryToken]      // <-- "Bùa chú" 2: Bảo mật chống giả mạo
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 1. Tìm đối tượng cần xóa trong Database
            var homePageMedia = await _context.HomePageMedias.FindAsync(id);

            if (homePageMedia != null)
            {
                try
                {
                    // 2. XỬ LÝ XÓA FILE ẢNH VẬT LÝ (Nếu là Slide/Banner có ảnh)
                    if (!string.IsNullOrEmpty(homePageMedia.ImageUrl))
                    {
                        // Trỏ đúng vào thư mục chứa ảnh mà ní đã lưu lúc Create/Edit
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string pathFile = Path.Combine(wwwRootPath, "images", "homepage", homePageMedia.ImageUrl);

                        // Kiểm tra xem file có thực sự tồn tại trên ổ cứng không rồi mới xóa
                        if (System.IO.File.Exists(pathFile))
                        {
                            System.IO.File.Delete(pathFile); // "Trảm" file ảnh ngay lập tức
                        }
                    }

                    // 3. XÓA BẢN GHI TRONG DATABASE
                    _context.HomePageMedias.Remove(homePageMedia);

                    // 4. LƯU THAY ĐỔI
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi (ví dụ file đang bị khóa), ní có thể log lỗi ở đây
                    ModelState.AddModelError("", "Ní ơi, có lỗi khi xóa file: " + ex.Message);
                    return View("Delete", homePageMedia);
                }
            }

            // 5. Xóa xong thì quay về trang danh sách
            return RedirectToAction(nameof(Index));
        }
        

        private bool HomePageMediaExists(int id) => _context.HomePageMedias.Any(e => e.Id == id);
    }
}