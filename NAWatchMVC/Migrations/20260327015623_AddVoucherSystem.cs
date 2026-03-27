using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NAWatchMVC.Migrations
{
    public partial class AddVoucherSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Tạo bảng Vouchers mới
            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    MaVoucher = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LoaiVoucher = table.Column<int>(type: "int", nullable: false),
                    LoaiGiamGia = table.Column<int>(type: "int", nullable: false),
                    GiaTriGiam = table.Column<double>(type: "float", nullable: false),
                    GiaTriDonHangToiThieu = table.Column<double>(type: "float", nullable: false),
                    GiamToiDa = table.Column<double>(type: "float", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoLuongToiDa = table.Column<int>(type: "int", nullable: false),
                    SoLuongDaDung = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vouchers", x => x.MaVoucher);
                });

            // 2. Thêm 2 cột mới vào bảng HoaDon ĐÃ CÓ SẴN
            migrationBuilder.AddColumn<string>(
                name: "MaVoucher",
                table: "HoaDon",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TienGiam",
                table: "HoaDon",
                type: "float",
                nullable: true);

            // 3. Tạo bảng ChiTietSuDungVoucher (Bảng cầu nối)
            migrationBuilder.CreateTable(
            name: "ChiTietSuDungVoucher",
            columns: table => new
            {
                MaKH = table.Column<string>(type: "nvarchar(20)", nullable: false),
                MaVoucher = table.Column<string>(type: "nvarchar(20)", nullable: false),
                NgayDung = table.Column<DateTime>(type: "datetime2", nullable: false),
                MaHD = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChiTietSuDungVoucher", x => new { x.MaKH, x.MaVoucher });
                table.ForeignKey(
                    name: "FK_ChiTietSuDungVoucher_HoaDon_MaHD",
                    column: x => x.MaHD,
                    principalTable: "HoaDon", // Sửa từ table thành principalTable
                    principalColumn: "MaHD",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ChiTietSuDungVoucher_KhachHang_MaKH",
                    column: x => x.MaKH,
                    principalTable: "KhachHang", // Sửa từ table thành principalTable
                    principalColumn: "MaKH");
                table.ForeignKey(
                    name: "FK_ChiTietSuDungVoucher_Vouchers_MaVoucher",
                    column: x => x.MaVoucher,
                    principalTable: "Vouchers", // Sửa từ table thành principalTable
                    principalColumn: "MaVoucher");
            });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietSuDungVoucher_MaHD",
                table: "ChiTietSuDungVoucher",
                column: "MaHD");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietSuDungVoucher_MaVoucher",
                table: "ChiTietSuDungVoucher",
                column: "MaVoucher");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChiTietSuDungVoucher");
            migrationBuilder.DropTable(name: "Vouchers");
            migrationBuilder.DropColumn(name: "MaVoucher", table: "HoaDon");
            migrationBuilder.DropColumn(name: "TienGiam", table: "HoaDon");
        }
    }
}