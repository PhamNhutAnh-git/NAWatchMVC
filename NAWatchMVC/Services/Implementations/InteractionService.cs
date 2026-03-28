using Microsoft.EntityFrameworkCore;
using NAWatchMVC.Controllers;
using NAWatchMVC.Data;
using NAWatchMVC.Services.Interfaces;

namespace NAWatchMVC.Services.Implementations
{
    public class InteractionService : IInteractionService
    {
        private readonly NawatchMvcContext _context; // Đổi thành tên DbContext của ní nha

        public InteractionService(NawatchMvcContext context)
        {
            _context = context;
        }

        public async Task Record(string maKh, int maHh, int type)
        {
            var interaction = new UserInteraction
            {
                MaKh = maKh,
                MaHh = maHh,
                InteractionType = type,
                CreatedAt = DateTime.Now
            };

            _context.UserInteractions.Add(interaction);
            await _context.SaveChangesAsync();
        }
        public async Task<List<HangHoa>> GetRecommendedProducts(string maKh)
        {
            // 1. Lấy toàn bộ tương tác của khách (Sắp xếp mới nhất lên đầu)
            var interactions = await _context.UserInteractions
                .Include(ui => ui.MaHhNavigation)
                .Where(ui => ui.MaKh == maKh)
                .OrderByDescending(ui => ui.CreatedAt)
                .ToListAsync();

            // Nếu khách mới hoàn toàn -> Trả về 6 món mới nhất
            if (!interactions.Any())
            {
                return await _context.HangHoas.OrderByDescending(h => h.MaHh).Take(6).ToListAsync();
            }

            var finalIds = new List<int>();

            // 2. LẤY 3 MÓN "VỪA MỚI..." (Đảm bảo không trùng ID)

            // - Lấy 1 món vừa XEM (Type 1)
            var lastView = interactions.FirstOrDefault(ui => ui.InteractionType == 1)?.MaHh;
            if (lastView.HasValue) finalIds.Add(lastView.Value);

            // - Lấy 1 món vừa TIM (Type 2) - Không trùng với món vừa xem
            var lastWish = interactions.FirstOrDefault(ui => ui.InteractionType == 2 && !finalIds.Contains(ui.MaHh))?.MaHh;
            if (lastWish.HasValue) finalIds.Add(lastWish.Value);

            // - Lấy 1 món vừa GIỎ HÀNG (Type 3) - Không trùng với 2 món trên
            var lastCart = interactions.FirstOrDefault(ui => ui.InteractionType == 3 && !finalIds.Contains(ui.MaHh))?.MaHh;
            if (lastCart.HasValue) finalIds.Add(lastCart.Value);

            // 3. TÍNH TOÁN "GU" CỦA KHÁCH (Dựa trên tổng điểm 5 loại)
            var topMaLoai = interactions
                .GroupBy(ui => ui.MaHhNavigation.MaLoai)
                .Select(g => new {
                    MaLoai = g.Key,
                    Score = g.Sum(x => x.InteractionType switch {
                        1 => 1,  // View
                        2 => 3,  // Wishlist
                        3 => 5,  // Cart
                        5 => 8,  // Review (Đánh giá)
                        4 => 10, // Purchase
                        _ => 0
                    })
                })
                .OrderByDescending(x => x.Score)
                .Select(x => x.MaLoai)
                .FirstOrDefault();

            // 4. LẤY THÊM SẢN PHẨM GỢI Ý ĐỂ ĐỦ 6 MÓN
            int needsMore = 6 - finalIds.Count;
            var suggestions = await _context.HangHoas
                .Where(h => h.MaLoai == topMaLoai && !finalIds.Contains(h.MaHh) && h.SoLuong > 0)
                .Take(needsMore)
                .ToListAsync();

            // 5. TỔNG HỢP VÀ TRẢ VỀ
            // Lấy thông tin chi tiết của 3 món đầu tiên
            var finalProducts = await _context.HangHoas
                .Where(h => finalIds.Contains(h.MaHh))
                .ToListAsync();

            // Sắp xếp lại danh sách finalProducts theo đúng thứ tự finalIds (View -> Tim -> Giỏ)
            finalProducts = finalIds.Select(id => finalProducts.FirstOrDefault(p => p.MaHh == id))
                                    .Where(p => p != null).ToList();

            // Thêm các món gợi ý vào sau
            finalProducts.AddRange(suggestions);

            // Fallback: Nếu vẫn chưa đủ 6 món (do MaLoai đó ít hàng quá) -> Bù thêm hàng mới nhất
            if (finalProducts.Count < 6)
            {
                var moreIds = finalProducts.Select(p => p.MaHh).ToList();
                var fillItems = await _context.HangHoas
                    .Where(h => !moreIds.Contains(h.MaHh) && h.SoLuong > 0)
                    .Take(6 - finalProducts.Count)
                    .ToListAsync();
                finalProducts.AddRange(fillItems);
            }

            return finalProducts.Take(6).ToList();
        }
    }
}