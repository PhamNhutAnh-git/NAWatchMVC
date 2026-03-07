using System;
using System.Collections.Generic;

namespace NAWatchMVC.Data;

public partial class GioHang
{
    public int MaGh { get; set; }

    public string MaKh { get; set; } = null!;

    public DateTime? NgayTao { get; set; }

    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    public virtual KhachHang MaKhNavigation { get; set; } = null!;
}
