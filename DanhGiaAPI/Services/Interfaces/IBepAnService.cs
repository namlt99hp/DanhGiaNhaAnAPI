using DanhGiaAPI.DTOs.BepAn;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IBepAnService
    {
        Task<List<BepAn>> DanhSachAsync(int? nhaThauId, string? trangThai);
        Task<BepAn> ChiTietAsync(int id);
        Task<BepAn> ThemAsync(BepAnRequest request);
        Task<BepAn> SuaAsync(int id, BepAnRequest request);
        Task XoaAsync(int id);
    }
}
