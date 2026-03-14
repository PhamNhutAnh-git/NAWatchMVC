namespace NAWatchMVC.ViewModels
{
    public class CartItemVM
    {
        public int MaHH { get; set; }
        public string TenHH { get; set; }
        public string Hinh { get; set; }

        public double DonGia { get; set; } // Giá chưa giảm
        public double GiamGia { get; set; }      // Phần trăm giảm (số nguyên, ví dụ 10)
        public int SoLuong { get; set; }

        // --- Logic tính toán nằm ở đây cho "sướng" ---

        // Giá bán sau khi đã giảm
        public double GiaBan => DonGia * (1 - GiamGia / 100.0);

        // Thành tiền = Giá bán * Số lượng
        public double ThanhTien => GiaBan * SoLuong;
    }
}