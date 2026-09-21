//using System.Collections.Generic;
using DanhGiaAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DiaDiemNhaAn> DiaDiemNhaAn { get; set; }
        public DbSet<KetQuaDanhGia> KetQuaDanhGia { get; set; }
        public DbSet<TieuChiDanhGia> TieuChiDanhGia { get; set; }

        public DbSet<DuLieuCom> DuLieuCom { get; set; }
        public DbSet<BuaAn> BuaAn { get; set; }
        public DbSet<KhungGioDanhGia> KhungGioDanhGia { get; set; }
        //public DbSet<LyDo> LyDo { get; set; }

        // Module đánh giá bếp ăn (mới) — xem 02. Phantich/schema_module_danh_gia_nha_an.sql
        // Entity nằm ở DanhGiaAPI.Entities (tách khỏi Models cũ), truy cập qua Repository.
        public DbSet<VaiTro> VaiTro { get; set; }
        public DbSet<Quyen> Quyen { get; set; }
        public DbSet<VaiTroQuyen> VaiTroQuyen { get; set; }
        public DbSet<PhongBan> PhongBan { get; set; }
        public DbSet<PhongBanLoaiPhieu> PhongBanLoaiPhieu { get; set; }
        public DbSet<NhaThau> NhaThau { get; set; }
        public DbSet<NguoiDung> NguoiDung { get; set; }
        public DbSet<NguoiDungVaiTro> NguoiDungVaiTro { get; set; }
        public DbSet<NguoiDungMauLuongKy> NguoiDungMauLuongKy { get; set; }
        public DbSet<NguoiDungPhieuQuyen> NguoiDungPhieuQuyen { get; set; }
        public DbSet<PhienDangNhap> PhienDangNhap { get; set; }
        public DbSet<ChuKyNguoiDung> ChuKyNguoiDung { get; set; }
        public DbSet<BepAn> BepAn { get; set; }
        public DbSet<NhomTieuChi> NhomTieuChi { get; set; }
        public DbSet<TieuChi> TieuChi { get; set; }

        // Giai đoạn 2 — hạ tầng dùng chung cho 4 loại phiếu (xem
        // 02. Phantich/Features.md Giai đoạn 2 + modules/LuongTrinhKy.md)
        public DbSet<BoDemSoHieu> BoDemSoHieu { get; set; }
        public DbSet<TepDinhKem> TepDinhKem { get; set; }
        public DbSet<MauLuongKy> MauLuongKy { get; set; }
        public DbSet<ChuKyPhieu> ChuKyPhieu { get; set; }
        public DbSet<NhatKyChinhSua> NhatKyChinhSua { get; set; }

        // Giai đoạn 3 — Phiếu (1): Kiểm tra VSATTP (xem
        // 02. Phantich/modules/Phieu1_KiemTraVSATTP.md)
        public DbSet<Phieu1KiemTra> Phieu1KiemTra { get; set; }
        public DbSet<Phieu1ChiTiet> Phieu1ChiTiet { get; set; }
        public DbSet<Phieu1KetLuan> Phieu1KetLuan { get; set; }

        // Giai đoạn 4 — Phiếu (2): Đánh giá chất lượng dịch vụ suất ăn (xem
        // 02. Phantich/modules/Phieu2_DanhGiaSuatAn.md)
        public DbSet<Phieu2DanhGia> Phieu2DanhGia { get; set; }
        public DbSet<Phieu2TieuChi> Phieu2TieuChi { get; set; }
        public DbSet<Phieu2KetQua> Phieu2KetQua { get; set; }
        public DbSet<Phieu2YKienNhaThau> Phieu2YKienNhaThau { get; set; }
        // Liên kết N-N Phiếu 2 <-> Nhà ăn — 1 phiếu có thể gộp nhiều nhà ăn
        // (xem Entities/Phieu2NhaAn.cs).
        public DbSet<Phieu2NhaAn> Phieu2NhaAn { get; set; }

        // Giai đoạn 5 — Phiếu (3): Báo cáo chất lượng dịch vụ suất ăn theo tháng (xem
        // 02. Phantich/modules/Phieu3_BaoCaoThang.md)
        public DbSet<Phieu3BaoCao> Phieu3BaoCao { get; set; }
        public DbSet<Phieu3Bang1Dong> Phieu3Bang1Dong { get; set; }
        public DbSet<Phieu3Bang2Dong> Phieu3Bang2Dong { get; set; }
        public DbSet<Phieu3Bang2GiaTri> Phieu3Bang2GiaTri { get; set; }
        public DbSet<Phieu3YKienNhaThau> Phieu3YKienNhaThau { get; set; }
        // "Đoạn" thời gian (khung Ngày+Bữa ăn + danh sách địa điểm) thay thế
        // suy luận "nhà ăn rõ ràng" cũ — xem Phieu3Service.
        public DbSet<Phieu3Doan> Phieu3Doan { get; set; }
        public DbSet<Phieu3DoanDiaDiem> Phieu3DoanDiaDiem { get; set; }

        // Giai đoạn 6 — Phiếu (4): Bảng tổng hợp đánh giá & phân bổ suất ăn (xem
        // 02. Phantich/modules/Phieu4_TongHopPhanBo.md)
        public DbSet<Phieu4TongHop> Phieu4TongHop { get; set; }
        public DbSet<Phieu4NhaThau> Phieu4NhaThau { get; set; }
        public DbSet<Phieu4Bang> Phieu4Bang { get; set; }
        public DbSet<Phieu4Dong> Phieu4Dong { get; set; }
        public DbSet<Phieu4GiaTri> Phieu4GiaTri { get; set; }
        // "Đoạn" thời gian CỦA TỪNG CỘT nhà thầu (Phieu4NhaThau) — thay thế
        // suy luận "địa điểm rõ ràng" cũ — xem Phieu4Service.
        public DbSet<Phieu4Doan> Phieu4Doan { get; set; }
        public DbSet<Phieu4DoanDiaDiem> Phieu4DoanDiaDiem { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NguoiDungVaiTro>().HasKey(x => new { x.NguoiDungId, x.VaiTroId });
            modelBuilder.Entity<VaiTroQuyen>().HasKey(x => new { x.VaiTroId, x.QuyenId });
            modelBuilder.Entity<NguoiDungMauLuongKy>().HasKey(x => new { x.NguoiDungId, x.MauLuongKyId });
            modelBuilder.Entity<NguoiDungPhieuQuyen>().HasKey(x => new { x.NguoiDungId, x.LoaiPhieu });
            modelBuilder.Entity<PhongBanLoaiPhieu>().HasKey(x => new { x.PhongBanId, x.LoaiPhieu });

            // Tên bảng có "_" (Phieu1_..., Phieu2_..., xem schema_module_danh_gia_nha_an.sql) khác
            // convention đặt tên entity C# (PascalCase liền, không "_") nên cần khai báo
            // ToTable tường minh — khác các bảng module Đăng nhập/hạ tầng chung ở trên vốn
            // trùng tên y hệt entity nên không cần Fluent API riêng.
            modelBuilder.Entity<Phieu1KiemTra>().ToTable("Phieu1_KiemTra");
            modelBuilder.Entity<Phieu1ChiTiet>().ToTable("Phieu1_ChiTiet");
            modelBuilder.Entity<Phieu1KetLuan>().ToTable("Phieu1_KetLuan");

            modelBuilder.Entity<Phieu2DanhGia>().ToTable("Phieu2_DanhGia");
            modelBuilder.Entity<Phieu2TieuChi>().ToTable("Phieu2_TieuChi");
            modelBuilder.Entity<Phieu2KetQua>().ToTable("Phieu2_KetQua");
            modelBuilder.Entity<Phieu2YKienNhaThau>().ToTable("Phieu2_YKienNhaThau");
            modelBuilder.Entity<Phieu2NhaAn>().ToTable("Phieu2_NhaAn");

            modelBuilder.Entity<Phieu3BaoCao>().ToTable("Phieu3_BaoCao");
            modelBuilder.Entity<Phieu3Bang1Dong>().ToTable("Phieu3_Bang1Dong");
            modelBuilder.Entity<Phieu3Bang2Dong>().ToTable("Phieu3_Bang2Dong");
            modelBuilder.Entity<Phieu3Bang2GiaTri>().ToTable("Phieu3_Bang2GiaTri");
            modelBuilder.Entity<Phieu3YKienNhaThau>().ToTable("Phieu3_YKienNhaThau");
            modelBuilder.Entity<Phieu3Doan>().ToTable("Phieu3_Doan");
            modelBuilder.Entity<Phieu3DoanDiaDiem>().ToTable("Phieu3_DoanDiaDiem");

            modelBuilder.Entity<Phieu4TongHop>().ToTable("Phieu4_TongHop");
            modelBuilder.Entity<Phieu4NhaThau>().ToTable("Phieu4_NhaThau");
            modelBuilder.Entity<Phieu4Bang>().ToTable("Phieu4_Bang");
            modelBuilder.Entity<Phieu4Dong>().ToTable("Phieu4_Dong");
            modelBuilder.Entity<Phieu4GiaTri>().ToTable("Phieu4_GiaTri");
            modelBuilder.Entity<Phieu4Doan>().ToTable("Phieu4_Doan");
            modelBuilder.Entity<Phieu4DoanDiaDiem>().ToTable("Phieu4_DoanDiaDiem");
        }
    }
}
