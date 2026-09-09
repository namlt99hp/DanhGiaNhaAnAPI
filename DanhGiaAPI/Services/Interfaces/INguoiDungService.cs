using DanhGiaAPI.DTOs.QuanLyTaiKhoan;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface INguoiDungService
    {
        Task<List<NguoiDungListItemDto>> DanhSachAsync(string? trangThai, int? phongBanId, int? nhaThauId);
        Task<NguoiDungListItemDto> ChiTietAsync(int id);
        Task<NguoiDungListItemDto> TaoTaiKhoanAsync(TaoTaiKhoanRequest request, int nguoiTaoId);
        Task<NguoiDungListItemDto> CapNhatProfileAsync(int id, CapNhatProfileRequest request);
        Task DoiMatKhauAsync(int id, DoiMatKhauRequest request);
        Task ResetMatKhauAsync(int id);
        Task DuyetAsync(int id, int nguoiDuyetId);
        Task TuChoiAsync(int id);
        Task KhoaAsync(int id);
        Task MoKhoaAsync(int id);
        Task CapNhatVaiTroAsync(int id, List<int> vaiTroIds);

        // Xóa VĨNH VIỄN 1 tài khoản (khác TuChoiAsync — chỉ xóa được tài khoản
        // CHO_DUYET). Chặn nếu tài khoản đã có dấu vết thật trong hệ thống (đã
        // lập/ký phiếu) để không phá vỡ thông tin "người tạo/người ký" hiển thị
        // trên các phiếu cũ — nên khóa (KhoaAsync) thay vì xóa trong trường hợp
        // đó. Xem NguoiDungService.XoaVinhVienAsync.
        Task XoaVinhVienAsync(int id, int nguoiThucHienId);

        // Phân quyền theo Phiếu (xem 02. Phantich/modules/VaiTro.md mục 9) —
        // độc lập hoàn toàn với CapNhatVaiTroAsync (quản trị hệ thống).
        Task<List<MauLuongKyKhaDungDto>> LuongKyKhaDungAsync(int id);
        Task CapNhatLuongKyAsync(int id, List<int> mauLuongKyIds);
        Task CapNhatPhieuQuyenAsync(int id, List<NguoiDungPhieuQuyenItemDto> danhSach);
    }
}
