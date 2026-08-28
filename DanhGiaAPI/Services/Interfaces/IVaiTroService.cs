using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IVaiTroService
    {
        Task<List<VaiTro>> DanhSachAsync();
        Task<VaiTro> ThemAsync(VaiTroRequest request);
        Task<VaiTro> SuaAsync(int id, VaiTroRequest request);
        Task XoaAsync(int id);
    }
}
