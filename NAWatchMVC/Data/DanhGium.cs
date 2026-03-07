using System;
using System.Collections.Generic;

namespace NAWatchMVC.Data;

public partial class DanhGium
{
    public int MaDg { get; set; }

    public int MaHh { get; set; }

    public string MaKh { get; set; } = null!;

    public DateTime? NgayDang { get; set; }

    public string? NoiDung { get; set; }

    public int? Sao { get; set; }

    public bool? TrangThai { get; set; }

    public virtual HangHoa MaHhNavigation { get; set; } = null!;

    public virtual KhachHang MaKhNavigation { get; set; } = null!;
}
