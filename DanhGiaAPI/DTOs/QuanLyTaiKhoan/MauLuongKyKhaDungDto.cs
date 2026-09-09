namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    // 1 dòng MauLuongKy nhìn từ góc độ "user X có gán được vào bước này
    // không" — dùng cho khối "Phân quyền theo Phiếu" ở form Quản lý tài
    // khoản (xem NguoiDungService.LuongKyKhaDungAsync).
    public class MauLuongKyKhaDungDto
    {
        public int MauLuongKyId { get; set; }
        public string LoaiPhieu { get; set; } = null!;
        public int BuocThuTu { get; set; }
        public string TenBuoc { get; set; } = null!;
        public string LoaiNguoiKy { get; set; } = null!; // PHONG_BAN / NHA_THAU / TRUC_TIEP

        // true nếu PHONG_BAN/NHA_THAU khớp phòng ban/nhà thầu hiện tại của
        // user (hoặc luôn true với TRUC_TIEP) — FE dùng để disable ô tick khi
        // không khớp thay vì để Admin gán nhầm.
        public bool DuDieuKienCauTruc { get; set; }

        public bool DaDuocGan { get; set; }
    }
}
