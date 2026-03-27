using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAWatchMVC.Data;
using NAWatchMVC.Helpers; // Để dùng được SetDouble

namespace NAWatchMVC.Controllers
{
    public class VoucherController : Controller
    {
        private readonly NawatchMvcContext _context;

        public VoucherController(NawatchMvcContext context)
        {
            _context = context;
        }
        [Authorize]
        [HttpPost]
        public IActionResult CheckVoucher(string code, double subtotal)
        {
            double discountAmount = 0;

            // --- BƯỚC MỚI: XÁC ĐỊNH PHÍ SHIP HIỆN TẠI DỰA TRÊN TỔNG TIỀN HÀNG ---
            double phiShipHienTai = (subtotal >= 1000000) ? 0 : 50000;

            // 0. Tìm mã trong DB
            var v = _context.Vouchers.FirstOrDefault(x => x.MaVoucher == code && x.TrangThai == true);

            // 00. Lấy mã khách hàng từ Claim
            var maKH = User.FindFirst("CustomerId")?.Value;

            // 1. Kiểm tra đăng nhập
            if (string.IsNullOrEmpty(maKH))
            {
                return Json(new { success = false, message = "Ní vui lòng đăng nhập để sử dụng mã giảm giá!" });
            }

            // 2. Kiểm tra các điều kiện cơ bản của Voucher
            if (v == null)
                return Json(new { success = false, message = "Mã giảm giá không tồn tại!" });

            if (v.NgayBatDau > DateTime.Now || v.NgayKetThuc < DateTime.Now)
                return Json(new { success = false, message = "Mã này đã hết hạn sử dụng rồi ní ơi!" });

            if (v.SoLuongToiDa > 0 && v.SoLuongDaDung >= v.SoLuongToiDa)
                return Json(new { success = false, message = "Mã này đã hết lượt dùng mất rồi!" });

            if (subtotal < v.GiaTriDonHangToiThieu)
                return Json(new { success = false, message = $"Đơn hàng phải từ {v.GiaTriDonHangToiThieu:N0}đ mới dùng được mã này!" });

            // Kiểm tra xem ông khách này đã dùng mã này chưa
            var daDung = _context.ChiTietSuDungVouchers.Any(x => x.MaKh == maKH && x.MaVoucher == code);
            if (daDung)
            {
                return Json(new { success = false, message = "Ní đã sử dụng mã này cho đơn hàng trước rồi!" });
            }

            // 3. Xác định số tiền gốc để tính giảm giá
            // Dùng phiShipHienTai thay vì số fix cứng để linh động theo đơn hàng
            double baseAmount = (v.LoaiVoucher == 1) ? phiShipHienTai : subtotal;

            // 4. Tính toán số tiền giảm
            if (v.LoaiGiamGia == 0) // Giảm số tiền cụ thể
            {
                discountAmount = v.GiaTriGiam;
            }
            else // Giảm theo %
            {
                discountAmount = baseAmount * (v.GiaTriGiam / 100.0);

                if (v.GiamToiDa > 0 && discountAmount > v.GiamToiDa)
                {
                    discountAmount = v.GiamToiDa;
                }
            }

            // Đặc biệt: Nếu là Voucher Ship, tiền giảm không được vượt quá phí ship thực tế
            // Nếu đơn trên 1 triệu (phiShipHienTai = 0) thì discountAmount chỗ này sẽ về 0
            if (v.LoaiVoucher == 1 && discountAmount > phiShipHienTai)
            {
                discountAmount = phiShipHienTai;
            }

            // 5. Lưu vào Session
            HttpContext.Session.SetString("VoucherCode", code);
            HttpContext.Session.SetDouble("VoucherDiscount", discountAmount);
            HttpContext.Session.SetInt32("VoucherType", v.LoaiVoucher);

            // 6. Trả kết quả về cho AJAX
            return Json(new
            {
                success = true,
                discount = discountAmount,
                loaiVoucher = v.LoaiVoucher,
                phiShip = phiShipHienTai, // Gửi về để giao diện cập nhật tiền Ship
                message = (v.LoaiVoucher == 1)
                          ? $"Áp dụng thành công! Ní được giảm {discountAmount:N0}đ phí vận chuyển."
                          : $"Áp dụng thành công! Ní được giảm {discountAmount:N0}đ tiền hàng."
            });
        }
    }
}