using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.Services.Interfaces;

public class RecommendationViewComponent : ViewComponent
{
    private readonly IInteractionService _interactionService;
    public RecommendationViewComponent(IInteractionService interactionService)
        => _interactionService = interactionService;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var maKh = UserClaimsPrincipal.FindFirst("CustomerId")?.Value;
        // Dù có đăng nhập hay không, hàm GetRecommendedProducts ở trên đã xử lý trả về list rồi
        var model = await _interactionService.GetRecommendedProducts(maKh ?? "");
        return View(model);
    }
}