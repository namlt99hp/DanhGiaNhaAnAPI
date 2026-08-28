namespace DanhGiaAPI.Services.Interfaces
{
    // Tra "phiếu X (LoaiPhieu/DoiTuongId) thuộc NhaThauId nào" — dùng để
    // ChuKyPhieuService kiểm tra quyền ký bước LoaiNguoiKy = NHA_THAU mà
    // không cần biết cấu trúc bảng riêng của từng loại phiếu (xem
    // modules/VaiTro.md mục 7.5). Trả về null nếu loại phiếu không có khái
    // niệm "1 nhà thầu duy nhất" (VD Phiếu 4 — tổng hợp nhiều nhà thầu) hoặc
    // không tìm thấy phiếu.
    public interface IPhieuNhaThauResolver
    {
        Task<int?> LayNhaThauIdAsync(string loaiPhieu, int doiTuongId);
    }
}
