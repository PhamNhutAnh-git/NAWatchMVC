using System.ComponentModel.DataAnnotations;
namespace NAWatchMVC.ViewModels
{
    public class RegisterVM
    {
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "Ní quên nhập Tên đăng nhập kìa!")]
        // Regex: Chỉ cho phép chữ cái (a-z, A-Z) và số (0-9), không khoảng trắng, không dấu
        [RegularExpression(@"^[a-zA-Z0-9]*$", ErrorMessage = "Tên đăng nhập phải viết liền, không dấu, không có ký tự đặc biệt nhé!")]
        [MaxLength(20, ErrorMessage = "Tối đa 20 ký tự thôi cho dễ nhớ.")]
        public string TenDangNhap { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "*")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; }

        [Display(Name = "Họ tên")]
        [Required(ErrorMessage = "*")]
        [MaxLength(50, ErrorMessage = "Tối đa 50 kí tự")]
        public string HoTen { get; set; }

        public bool GioiTinh { get; set; } = true;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? NgaySinh { get; set; }

        [Display(Name = "Địa chỉ")]
        [MaxLength(60, ErrorMessage = "Tối đa 60 kí tự")]
        public string DiaChi { get; set; }

        [Display(Name = "Điện thoại")]
        [MaxLength(24, ErrorMessage = "Tối đa 24 kí tự")]
        [RegularExpression(@"0\d{9}", ErrorMessage = "Số điện thoại chưa đúng định dạng")]
        public string DienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Chưa đúng định dạng email")]
        public string Email { get; set; }

        public string? Hinh { get; set; }
    }
}
