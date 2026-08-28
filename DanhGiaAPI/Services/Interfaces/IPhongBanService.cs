using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IPhongBanService
    {
        Task<List<PhongBan>> DanhSachAsync(bool? dangHoatDong);
        Task<PhongBan> ThemAsync(PhongBanRequest request);
        Task<PhongBan> SuaAsync(int id, PhongBanRequest request);
        Task XoaAsync(int id);
    }
}
