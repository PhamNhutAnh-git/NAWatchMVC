using System.ComponentModel.DataAnnotations;

namespace NAWatchMVC.Data
{
    public class Voucher
    {
        [Key]
        [StringLength(20)]
        public string MaVoucher { get; set; } = null!;

        public int LoaiVoucher { get; set; } // 0: Tiền hàng, 1: Phí ship
        public int LoaiGiamGia { get; set; } // 0: Tiền mặt (đ), 1: Phần trăm (%)
        public double GiaTriGiam { get; set; }
        public double GiaTriDonHangToiThieu { get; set; } = 0;
        public double GiamToiDa { get; set; } = 0;
        public DateTime NgayBatDau { get; set; } = DateTime.Now;
        public DateTime NgayKetThuc { get; set; }
        public int SoLuongToiDa { get; set; } = 0;
        public int SoLuongDaDung { get; set; } = 0;
        public bool TrangThai { get; set; } = true;
    }
}