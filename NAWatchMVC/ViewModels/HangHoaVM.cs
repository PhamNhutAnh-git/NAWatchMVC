using System;
namespace NAWatchMVC.ViewModels
{
    public class HangHoaVM
    {
        public int MaHh { get; set; } // Để làm link chi tiết sản phẩm
        public string TenHh { get; set; } // Tên đầy đủ như TGDĐ
        public string Hinh { get; set; } // Tên file ảnh trong wwwroot/Hinh/HangHoa

        // Giá cả (Sử dụng double? vì Database của bạn cho phép Null)
        public double DonGia { get; set; } //gia goc
        public double GiamGia { get; set; } // Phần trăm giảm (Ví dụ: 10 cho 10%)

        // Tính toán giá bán sau khi giam
        public double GiaBan => DonGia * (1 - GiamGia / 100.0);

        // Thông số kỹ thuật (3 dòng xám xám trong ảnh)
        public string LoaiMay { get; set; } // Ví dụ: Pin (Quartz)
        public string DoRongDay { get; set; } // Ví dụ: 8.1 mm
        public string ChatLieuKinh { get; set; } // Ví dụ: Nhựa

        // Đánh giá và nhãn
        public int DiemDanhGia { get; set; } // Số sao (1-5)
        public int SoLuongBan { get; set; } // "Đã bán X"
        public string NhanUuDai { get; set; } // Ví dụ: "Trả chậm 0%" hoặc "Mới về"

        // Tên loại/Hãng để hiển thị thêm nếu cần
        public string TenLoai { get; set; }
    }
    public class ChiTietHangHoaVM
    {
        // 1. Thông tin cơ bản và định danh
        public int MaHh { get; set; }
        public string TenHh { get; set; }
        public string TenAlias { get; set; }
        public int MaLoai { get; set; } // Giữ kiểu dữ liệu theo Database của bạn
        public string MaNcc { get; set; }
        public string Hinh { get; set; }

        // 2. Giá cả và Ưu đãi (Logic GiaBan đã được tích hợp)
        public double DonGia { get; set; }
        public double GiamGia { get; set; }
        public double GiaBan => DonGia * (1 - GiamGia / 100.0);
        public string NhanUuDai { get; set; }

        // 3. Thông tin kho hàng và tương tác
        public int SoLuong { get; set; }
        public int SoLuongBan { get; set; }
        public int SoLanXem { get; set; }
        public DateTime NgaySx { get; set; }
        public int DiemDanhGia { get; set; }
        public int ThoiGianPin { get; set; }

        // 4. Mô tả sản phẩm
        public string MoTa { get; set; }
        public string MoTaNgan => MoTa?.Length > 150 ? MoTa.Substring(0, 150) + "..." : MoTa;

        // 5. Thông số kỹ thuật chi tiết (Bảng thông số)
        public string GioiTinh { get; set; }
        public string DuongKinhMat { get; set; }
        public string ChatLieuDay { get; set; }
        public string DoRongDay { get; set; }
        public string ChatLieuKhungVien { get; set; }
        public string ChatLieuKinh { get; set; }
        public string TenBoMay { get; set; }
        public string ChongNuoc { get; set; }
        public string TienIch { get; set; }
        public string NguonNangLuong { get; set; }
        public string LoaiMay { get; set; }
        public string BoSuuTap { get; set; }
        public string XuatXu { get; set; }

        // 6. Thông tin từ các bảng liên kết
        public string TenLoai { get; set; }
        public string TenNcc { get; set; }
        // Thêm dòng này để chứa danh sách sản phẩm cùng loại
        public List<HangHoaVM> SanPhamTuongTu { get; set; }
    }
}