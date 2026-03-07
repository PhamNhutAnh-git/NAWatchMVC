namespace NAWatchMVC.ViewModels
{
    public class HangHoaVM
    {
        public int MaHh { get; set; } // Để làm link chi tiết sản phẩm
        public string TenHh { get; set; } // Tên đầy đủ như TGDĐ
        public string Hinh { get; set; } // Tên file ảnh trong wwwroot/Hinh/HangHoa

        // Giá cả (Sử dụng double? vì Database của bạn cho phép Null)
        public double DonGia { get; set; }
        public double GiamGia { get; set; } // % giảm giá (ví dụ: 10)

        // Tính toán giá cũ (Giá gốc trước khi giảm)
        //public double GiaGoc => DonGia * (1 + (double)GiamGia / 100);
        public double GiaGoc => GiamGia > 0 ? DonGia / (1 - (double)GiamGia / 100) : DonGia;

        // Thông số kỹ thuật (3 dòng xám xám trong ảnh)
        public string LoaiMay { get; set; } // Ví dụ: Pin (Quartz)
        public string DoRongDay { get; set; } // Ví dụ: 8.1 mm
        public string ChatLieuKinh { get; set; } // Ví dụ: Nhựa

        // Đánh giá và nhãn
        public int DiemDanhGia { get; set; } // Số sao (1-5)
        public int SoLuongBan { get; set; } // "Đã bán X"
        public string NhanUuDai { get; set; } // Ví dụ: "Trả chậm 0%" hoặc "Mới về"

        // Tên loại/Hãng để hiển thị thêm nếu cần
        public string TenLoai { get; set; }
    }
}