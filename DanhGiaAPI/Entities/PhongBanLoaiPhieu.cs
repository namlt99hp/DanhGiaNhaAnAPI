namespace DanhGiaAPI.Entities
{
    // "Trần" cấu trúc: Phòng ban này ĐƯỢC PHÉP cấu hình Đánh giá/Quản lý tiêu
    // chí/Ký cho LoaiPhieu này — KHÔNG phải quyền thật của 1 tài khoản cụ thể
    // (đó vẫn là NguoiDungPhieuQuyen/NguoiDungMauLuongKy). Dùng làm trần khi
    // Admin gán quyền cho user thuộc phòng ban đó (xem NguoiDungService).
    //
    // Phòng ban CHƯA có dòng nào ở đây = KHÔNG giới hạn (mọi loại phiếu) — an
    // toàn khi rollout, tránh khóa nhầm phòng ban chưa kịp cấu hình.
    //
    // Nhà thầu KHÔNG cấu hình ở đây — quy tắc nhà thầu cố định toàn hệ thống,
    // xem NguoiDungService.LOAI_PHIEU_NHA_THAU_DUOC_KY.
    public class PhongBanLoaiPhieu
    {
        public int PhongBanId { get; set; }
        public string LoaiPhieu { get; set; } = null!; // PHIEU1..PHIEU4
    }
}
