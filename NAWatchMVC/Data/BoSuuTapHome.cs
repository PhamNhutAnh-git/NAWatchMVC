using System.ComponentModel.DataAnnotations;

namespace NAWatchMVC.Data
{
    public class BoSuuTapHome
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên này phải khớp với chữ trong cột BoSuuTap của bảng HangHoa")]
        public string CollectionName { get; set; } // Ví dụ: "Vintage", "G-Shock"

        public string? BackgroundImage { get; set; } // Ảnh nền BST

        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}