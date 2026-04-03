using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting; // Cần cái này để lấy đường dẫn wwwroot
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;

namespace NAWatchMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")] // Khóa chặt cửa cho Admin
    public class NewsArticlesController : Controller
    {
        private readonly NawatchMvcContext _context;
        private readonly IWebHostEnvironment _hostEnvironment; // Inject môi trường web

        public NewsArticlesController(NawatchMvcContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Admin/NewsArticles
        public async Task<IActionResult> Index()
        {
            return View(await _context.NewsArticles.OrderByDescending(x => x.PublishedDate).ToListAsync());
        }

        // GET: Admin/NewsArticles/Create
        public IActionResult Create() => View();

        // POST: Admin/NewsArticles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsArticle newsArticle, IFormFile fHinh)
        {
            if (ModelState.IsValid)
            {
                // 1. XỬ LÝ LƯU ẢNH ĐẠI DIỆN
                if (fHinh != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fHinh.FileName);
                    string path = Path.Combine(_hostEnvironment.WebRootPath, "Hinh", "TinTuc", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await fHinh.CopyToAsync(stream);
                    }
                    newsArticle.ImageUrl = fileName; // Lưu tên file vào DB
                }

                _context.Add(newsArticle);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(newsArticle);
        }

        // GET: Admin/NewsArticles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var newsArticle = await _context.NewsArticles.FindAsync(id);
            if (newsArticle == null) return NotFound();
            return View(newsArticle);
        }

        // POST: Admin/NewsArticles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Edit(int id, NewsArticle newsArticle, IFormFile? fHinh)
        {
            if (id != newsArticle.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. XỬ LÝ ẢNH BANNER
                    if (fHinh != null)
                    {
                        // Nếu có ảnh mới thì xóa ảnh cũ cho sạch server (tùy ní muốn hay không)
                        // if (!string.IsNullOrEmpty(newsArticle.ImageUrl)) { ... xóa file ... }

                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fHinh.FileName);
                        string path = Path.Combine(_hostEnvironment.WebRootPath, "Hinh", "TinTuc", fileName);

                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await fHinh.CopyToAsync(stream);
                        }
                        newsArticle.ImageUrl = fileName;
                    }
                    // Nếu fHinh == null, ImageUrl sẽ giữ nguyên giá trị từ thẻ Hidden gửi lên

                    _context.Update(newsArticle);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NewsArticleExists(newsArticle.Id)) return NotFound();
                    else throw;
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors)
            {
                Console.WriteLine(error.ErrorMessage); // Nó sẽ hiện lỗi ở cửa sổ Output của Visual Studio
            }
            return View(newsArticle);
        }


        // GET: Admin/NewsArticles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            // 1. Kiểm tra xem ní có truyền Id lên không
            if (id == null)
            {
                return NotFound();
            }

            // 2. Tìm bài viết trong Database theo Id
            var newsArticle = await _context.NewsArticles
                .FirstOrDefaultAsync(m => m.Id == id);

            // 3. Nếu không tìm thấy bài viết (ví dụ xóa rồi) thì báo lỗi 404
            if (newsArticle == null)
            {
                return NotFound();
            }

            // 4. Có dữ liệu rồi thì "đẩy" ra View thôi!
            return View(newsArticle);
        }
        // POST: Admin/NewsArticles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var newsArticle = await _context.NewsArticles.FindAsync(id);
            if (newsArticle != null)
            {
                // 3. XÓA FILE ẢNH TRONG THƯ MỤC KHI XÓA BÀI (Cho sạch server)
                if (!string.IsNullOrEmpty(newsArticle.ImageUrl))
                {
                    var imagePath = Path.Combine(_hostEnvironment.WebRootPath, "Hinh", "TinTuc", newsArticle.ImageUrl);
                    if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
                }
                _context.NewsArticles.Remove(newsArticle);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        
        [HttpPost]  
        public IActionResult UploadImageEditor(IFormFile file)
        {
            if (file == null) return Json(new { location = "" });

            // 1. Đặt tên file độc nhất
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            // 2. Đường dẫn lưu vào thư mục Uploads (nhớ tạo thư mục này nha ní!)
            string path = Path.Combine(_hostEnvironment.WebRootPath, "Hinh", "TinTuc", "Uploads", fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // 3. Trả về đường dẫn để TinyMCE hiển thị ảnh ngay trong bài viết
            return Json(new { location = $"/Hinh/TinTuc/Uploads/{fileName}" });
        }
        private bool NewsArticleExists(int id) => _context.NewsArticles.Any(e => e.Id == id);
    }
}