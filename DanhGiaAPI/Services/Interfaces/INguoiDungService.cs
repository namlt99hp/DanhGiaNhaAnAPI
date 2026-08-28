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
    }
}
