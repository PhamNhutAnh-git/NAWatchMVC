using System.ComponentModel.DataAnnotations;

namespace NAWatchMVC.Data
{
    public class HomePageMedia
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; }

        public string? ImageUrl { get; set; } // Đường dẫn ảnh trong wwwroot

        public string? YoutubeUrl { get; set; } // Link Embed YouTube

        [Required]
        public string MediaType { get; set; } // 'Slide', 'BannerChinh', 'Video'

        public string? NavigationLink { get; set; } // Link khi click vào

        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public string ViTri { get; set; } = "Top";
    }
}