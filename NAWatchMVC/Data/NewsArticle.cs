using System.ComponentModel.DataAnnotations;

namespace NAWatchMVC.Data
{
    public class NewsArticle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public string Content { get; set; } // Nội dung HTML

        public string? Summary { get; set; } // Tóm tắt ngắn

        public DateTime PublishedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}