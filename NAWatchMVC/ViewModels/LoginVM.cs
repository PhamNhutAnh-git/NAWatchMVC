using System.ComponentModel.DataAnnotations;

namespace NAWatchMVC.ViewModels // 1. Đã đổi Namespace cho đúng project NAWatch
{
    public class LoginVM
    {
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "Ní quên nhập tên đăng nhập kìa!")]
        [MaxLength(20, ErrorMessage = "Tối đa 20 kí tự thôi nhé.")]
        // 2. Đổi từ UserName thành TenDangNhap cho khớp với DB và Controller
        public string TenDangNhap { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Mật khẩu đâu ní ơi?")]
        [DataType(DataType.Password)]
        // 3. Đổi thành MatKhau để đồng bộ với RegisterVM
        public string MatKhau { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}