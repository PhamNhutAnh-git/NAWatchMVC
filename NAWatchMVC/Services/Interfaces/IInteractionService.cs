using NAWatchMVC.Data;

namespace NAWatchMVC.Services.Interfaces
{
    public interface IInteractionService
    {
        // Đây là "hợp đồng": Bất kỳ ai thực hiện dịch vụ này 
        // ĐỀU PHẢI có hàm Record để ghi lại tương tác
        Task Record(string maKh, int maHh, int type);
        // 2. THÊM DÒNG NÀY VÀO NÈ NÍ
        Task<List<HangHoa>> GetRecommendedProducts(string maKh);
    }
}
