namespace NAWatchMVC.ViewModels // Đổi namespace cho khớp project mới
{
    public class CartItemVM
    {
        public int MaHH { get; set; }
        public string TenHH { get; set; }
        public string Hinh { get; set; }
        public double DonGia { get; set; }
        public int SoLuong { get; set; }

        // Dùng dấu => để tự động tính thành tiền, cực kỳ tiện lợi
        public double ThanhTien => DonGia * SoLuong;
    }
}