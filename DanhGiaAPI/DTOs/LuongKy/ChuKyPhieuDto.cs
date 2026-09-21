namespace DanhGiaAPI.DTOs.LuongKy
{
    // Đúng cấu trúc Entities/ChuKyPhieu.cs, thêm DuongDanChuKy (resolve sẵn từ
    // ChuKyId) để FE render ảnh chữ ký khi hiển thị tiến độ ký — xem
    // ChuKyPhieuService.TienDoKyAsync.
    public class ChuKyPhieuDto
    {
        public int Id { get; set; }
        public string LoaiDoiTuong { get; set; } = null!;
        public int DoiTuongId { get; set; }
        public int BuocThuTu { get; set; }
        public string? TenBuoc { get; set; }
        public int? NguoiKyId { get; set; }
        // Họ tên resolve sẵn cho NguoiKyId/NguoiKyDuKienId — KHÔNG được tra
        // qua "danh sách người đủ điều kiện ký" ở FE (khác khái niệm: đó là
        // ai CÓ THỂ ký ngay bây giờ, còn đây là ai ĐÃ ký/được chỉ định). Bước
        // "Người đánh giá" (BuocThuTu=0, tự tạo DA_DUYET, không gắn
        // MauLuongKy nào) sẽ luôn có danh sách đủ điều kiện RỖNG nên bắt buộc
        // phải resolve tên ở đây.
        public string? NguoiKyHoTen { get; set; }
        public int? NguoiKyDuKienId { get; set; }
        public string? NguoiKyDuKienHoTen { get; set; }
        public int? ChuKyId { get; set; }
        public string TrangThai { get; set; } = null!;
        public string? GhiChu { get; set; }
        public DateTime? NgayKy { get; set; }
        public int LuotKy { get; set; }

        // Ảnh chữ ký (đường dẫn tương đối, xem ApiRootV2 ở FE) ĐÃ DÙNG khi ký
        // bước này — snapshot theo ChuKyId lúc ký, không đổi theo nếu người
        // đó sau này đổi chữ ký "đang sử dụng". NULL khi: chưa ký, người ký
        // là tài khoản NHÀ THẦU (nhà thầu không quản lý ảnh chữ ký trong hệ
        // thống — FE chỉ hiện icon √), hoặc người ký nội bộ chưa từng upload
        // chữ ký nào. FE coi NULL = hiện icon √ thay vì ảnh.
        public string? DuongDanChuKy { get; set; }
    }
}
