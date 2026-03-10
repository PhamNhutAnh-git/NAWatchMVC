using System.ComponentModel.DataAnnotations;

namespace NAWatchMVC.ViewModels
{
    public class DoiMatKhauVM
    {
        [Required(ErrorMessage = "Ní phải nhập mật khẩu hiện tại chứ!")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu cũ")]
        public string MatKhauCu { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới đâu ní?")]
        [MinLength(6, ErrorMessage = "Ít nhất 6 ký tự cho an toàn nhé.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string MatKhauMoi { get; set; }

        [Required(ErrorMessage = "Nhập lại mật khẩu mới cái nè.")]
        [DataType(DataType.Password)]
        [Compare("MatKhauMoi", ErrorMessage = "Hai cái mật khẩu mới này không khớp nhau rồi!")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string XacNhanMatKhau { get; set; }
    }
}