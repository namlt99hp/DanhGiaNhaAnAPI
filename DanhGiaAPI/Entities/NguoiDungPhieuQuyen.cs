namespace DanhGiaAPI.Entities
{
    // Quyền thao tác NỘI DUNG của 1 loại phiếu — KHÔNG phải quyền ký (xem
    // NguoiDungMauLuongKy) và KHÔNG phải quyền quản trị hệ thống (xem
    // VaiTro/VaiTroQuyen). 3 khái niệm này độc lập hoàn toàn với nhau.
    public class NguoiDungPhieuQuyen
    {
        public int NguoiDungId { get; set; }
        public string LoaiPhieu { get; set; } = null!; // PHIEU1..PHIEU4

        // Được tạo mới / nhập liệu / sửa nội dung phiếu (không phải ký).
        public bool DuocDanhGia { get; set; }

        // Được thêm/sửa/xóa tiêu chí áp dụng riêng cho loại phiếu này.
        public bool DuocQuanLyTieuChi { get; set; }
    }
}
