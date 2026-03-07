using System;
using System.Collections.Generic;

namespace NAWatchMVC.Data;

public partial class ChiTietGioHang
{
    public int MaCtgh { get; set; }

    public int MaGh { get; set; }

    public int MaHh { get; set; }

    public int? SoLuong { get; set; }

    public DateTime? NgayThem { get; set; }

    public virtual GioHang MaGhNavigation { get; set; } = null!;

    public virtual HangHoa MaHhNavigation { get; set; } = null!;
}
