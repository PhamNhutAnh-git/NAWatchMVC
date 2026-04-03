using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.Services.Interfaces;
using NAWatchMVC.ViewModels;

public class RecommendationViewComponent : ViewComponent
{
    private readonly IInteractionService _interactionService;
    public RecommendationViewComponent(IInteractionService interactionService)
        => _interactionService = interactionService;

    public async Task<IViewComponentResult> InvokeAsync(bool isHome = false)
    {
        var maKh = UserClaimsPrincipal.FindFirst("CustomerId")?.Value;
        // Dù có đăng nhập hay không, hàm GetRecommendedProducts ở trên đã xử lý trả về list rồi
        //var model = await _interactionService.GetRecommendedProducts(maKh ?? "");
        // 1. Lấy dữ liệu thô (List<HangHoa>) từ Service
        var rawProducts = await _interactionService.GetRecommendedProducts(maKh ?? "");

        // 2. PHÙ PHÉP: Chuyển từ HangHoa sang HangHoaVM
        var model = rawProducts
            .Take(5)
            .Select(h => new HangHoaVM
        {
            MaHh = h.MaHh,
            TenHh = h.TenHh,
            Hinh = h.Hinh,
            DonGia = h.DonGia ?? 0,
            GiamGia = h.GiamGia ?? 0,
            DiemDanhGia = (int)(h.DiemDanhGia ?? 5),
            SoLuongBan = h.SoLuongBan ?? 0,
            isHome = isHome
        }).ToList();
        if (isHome) return View("RecommendationHome", model);
        return View( model);
    }
}