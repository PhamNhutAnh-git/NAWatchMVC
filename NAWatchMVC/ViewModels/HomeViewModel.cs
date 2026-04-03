using NAWatchMVC.Data;
using NAWatchMVC.ViewModels;

namespace NAWatchMVC.Models
{
    public class HomeViewModel
    {
        public List<HomePageMedia> Slides { get; set; } = new();
        public List<HomePageMedia> Banners { get; set; } = new();
        public List<Voucher> Vouchers { get; set; } = new();
        public List<NewsArticle> News { get; set; } = new();
        
        // THÊM DÒNG NÀY NÈ NÍ!
        public List<HomePageMedia> Videos { get; set; } = new();
        // Cái này để chứa danh sách các Bộ sưu tập kèm sản phẩm bên trong
        public List<CollectionData> Collections { get; set; } = new();
        // 4 Danh sách sản phẩm theo mã loại ní gửi
        public List<HangHoaVM> NamProducts { get; set; } = new List<HangHoaVM>();
        public List<HangHoaVM> NuProducts { get; set; } = new List<HangHoaVM>();
        public List<HangHoaVM> UnisexProducts { get; set; } = new List<HangHoaVM>();
        public List<HangHoaVM> TreEmProducts { get; set; } = new List<HangHoaVM>();
        // THÊM DÒNG NÀY VÀO ĐỂ HẾT LỖI NÈ NÍ:
        public List<HangHoaVM> MonthlyProducts { get; set; } = new List<HangHoaVM>();
        // Danh sách các khối hãng sản xuất
        public List<BrandProductSection> BrandSections { get; set; } = new List<BrandProductSection>();
        
        // Thêm 4 cái "ăng-ten" thu sóng Video này vào:
        public string VideoIdNam { get; set; }
        public string VideoIdNu { get; set; }
        public string VideoIdUnisex { get; set; }
        public string VideoIdTreEm { get; set; }
        public string VideoIdIntro { get; set; }
        public string VideoIdIntro1 { get; set; }

    }

    public class CollectionData
    {
        public string Name { get; set; }
        public string? Image { get; set; }
        //public List<HangHoa> Products { get; set; } = new();
        // SỬA CHỖ NÀY: Thay HangHoa thành HangHoaVM
        public List<HangHoaVM> Products { get; set; } = new List<HangHoaVM>();
    }
    public class BrandProductSection
    {
        public string BrandName { get; set; }
        public string BrandLogo { get; set; } // Ảnh banner bên trái
        public List<HangHoaVM> Products { get; set; }
    }
}