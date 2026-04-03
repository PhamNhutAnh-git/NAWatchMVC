using System;
using System.Collections.Generic;

namespace NAWatchMVC.Data;

public partial class GopY
{
    public string MaGy { get; set; } = null!;

    public string? HoTen { get; set; }

    public string? Email { get; set; }

    public string? DienThoai { get; set; }

    public string NoiDung { get; set; } = null!;

    public DateTime? NgayGy { get; set; }

    public bool? IsRead { get; set; }
    public bool IsVisible { get; set; } = true; // Mặc định là hiện lên luôn
}
