using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Data;

namespace NAWatchMVC.Controllers
{
    public class NewsController : Controller
    {
        private readonly NawatchMvcContext _context;

        public NewsController(NawatchMvcContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10; // Mỗi trang hiện 6 bài cho đẹp
            var query = _context.NewsArticles.Where(x => x.IsActive).OrderByDescending(x => x.PublishedDate);

            var totalNews = await query.CountAsync();
            var newsList = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalNews / pageSize);

            return View(newsList);
        }
        // Action này xử lý link /News/Details/{id}
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var article = await _context.NewsArticles
                .FirstOrDefaultAsync(m => m.Id == id);

            if (article == null) return NotFound();

            return View(article);
        }
    }
}