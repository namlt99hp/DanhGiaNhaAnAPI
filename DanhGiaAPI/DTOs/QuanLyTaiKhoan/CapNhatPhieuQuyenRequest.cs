namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    // Thay thế TOÀN BỘ quyền thao tác nội dung phiếu (đánh giá/quản lý tiêu
    // chí) của 1 tài khoản — không cần liệt kê đủ cả 4 LoaiPhieu, thiếu dòng
    // nào coi như dòng đó không có quyền gì (cả 2 cờ đều false).
    public class CapNhatPhieuQuyenRequest
    {
        public List<NguoiDungPhieuQuyenItemDto> DanhSach { get; set; } = new();
    }
}
