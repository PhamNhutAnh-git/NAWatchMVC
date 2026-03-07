using System;
using System.Collections.Generic;

namespace NAWatchMVC.Data;

public partial class UserInteraction
{
    public int Id { get; set; }

    public string MaKh { get; set; } = null!;

    public int MaHh { get; set; }

    public int? InteractionType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual HangHoa MaHhNavigation { get; set; } = null!;

    public virtual KhachHang MaKhNavigation { get; set; } = null!;
}
