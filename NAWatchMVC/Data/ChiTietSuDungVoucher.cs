using System.ComponentModel.DataAnnotations.Schema;

namespace NAWatchMVC.Data
{
    public class ChiTietSuDungVoucher
    {
        public string MaKh { get; set; } = null!;
        public string MaVoucher { get; set; } = null!;
        public DateTime NgayDung { get; set; } = DateTime.Now;
        public int MaHd { get; set; }

        [ForeignKey("MaKh")]
        public virtual KhachHang? MaKhNavigation { get; set; }

        [ForeignKey("MaVoucher")]
        public virtual Voucher? MaVoucherNavigation { get; set; }

        [ForeignKey("MaHd")]
        public virtual HoaDon? MaHdNavigation { get; set; }
    }
}