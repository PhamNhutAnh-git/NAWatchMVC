using System.ComponentModel.DataAnnotations;

namespace NAWatchMVC.ViewModels
{
    public class EditProfileVM
    {
        // Khóa chính để tìm khách hàng, để ẩn hoặc ReadOnly nhé ní
        public string MaKh { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string HoTen { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        public string DienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public string DiaChi { get; set; }

        public bool? GioiTinh { get; set; }

        // Dùng cái này để nhận file ảnh
        public IFormFile FileHinh { get; set; }

        public string HinhCu { get; set; } // Để giữ lại ảnh cũ nếu khách không đổi ảnh mới
    }
}