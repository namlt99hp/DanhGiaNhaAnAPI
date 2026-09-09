using DanhGiaAPI.DTOs.QuanLyTaiKhoan;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IVaiTroService
    {
        Task<List<VaiTroResponseDto>> DanhSachAsync();
        Task<VaiTroResponseDto> ThemAsync(VaiTroRequest request);
        Task<VaiTroResponseDto> SuaAsync(int id, VaiTroRequest request);
        Task XoaAsync(int id);
    }
}
