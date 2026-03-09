using System;
using System.Collections.Generic;

namespace NAWatchMVC.Data;

public partial class HangHoa
{
    public int MaHh { get; set; }

    public string TenHh { get; set; } = null!;

    public string? TenAlias { get; set; }

    public int MaLoai { get; set; }

    public string MaNcc { get; set; } = null!;

    public double? DonGia { get; set; }

    public double? GiamGia { get; set; }

    public string? Hinh { get; set; }

    public DateTime? NgaySx { get; set; }

    public int? SoLanXem { get; set; }

    public string? MoTa { get; set; }

    public int? SoLuongBan { get; set; }

    public int? SoLuong { get; set; }

    public string? GioiTinh { get; set; }

    public string? DuongKinhMat { get; set; }

    public string? ChatLieuDay { get; set; }

    public string? DoRongDay { get; set; }

    public string? ChatLieuKhungVien { get; set; }

    public string? ChatLieuKinh { get; set; }

    public string? TenBoMay { get; set; }

    public string? ChongNuoc { get; set; }

    public string? TienIch { get; set; }

    public string? NguonNangLuong { get; set; }

    public string? LoaiMay { get; set; }

    public string? BoSuuTap { get; set; }

    public string? XuatXu { get; set; }

    public double? DiemDanhGia { get; set; }
    public int? ThoiGianPin { get; set; } // Dùng int? vì trong DB có thể để Null

    public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; } = new List<ChiTietGioHang>();

    public virtual ICollection<ChiTietHd> ChiTietHds { get; set; } = new List<ChiTietHd>();

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual Loai MaLoaiNavigation { get; set; } = null!;

    public virtual NhaCungCap MaNccNavigation { get; set; } = null!;

    public virtual ICollection<UserInteraction> UserInteractions { get; set; } = new List<UserInteraction>();

    public virtual ICollection<YeuThich> YeuThiches { get; set; } = new List<YeuThich>();
}
