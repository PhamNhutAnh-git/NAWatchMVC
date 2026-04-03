using System.ComponentModel.DataAnnotations;

namespace NAWatchMVC.ViewModels
{
    public class GopYVM
    {
        [Required(ErrorMessage = "Bạn vui lòng cho shop xin cái tên để dễ xưng hô nhé!")]
        [StringLength(50)]
        public string? HoTen { get; set; }

        [Required(ErrorMessage = "Đừng quên nhập email nhé.")]
        [EmailAddress(ErrorMessage = "Email này nhìn lạ quá, ní check lại xem.")]
        public string? Email { get; set; }

        [RegularExpression(@"0\d{9,10}", ErrorMessage = "Số điện thoại này chưa chuẩn!")]
        public string? DienThoai { get; set; }

        [Required(ErrorMessage = "Bạn muốn nhắn nhủ gì thì ghi vào đây nè.")]
        [MinLength(10, ErrorMessage = "Góp ý đang khá ngắn? Viết thêm xíu.")]
        public string NoiDung { get; set; } = null!;
    }
}
