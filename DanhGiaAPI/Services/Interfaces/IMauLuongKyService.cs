using DanhGiaAPI.DTOs.LuongKy;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IMauLuongKyService
    {
        Task<List<MauLuongKy>> DanhSachAsync(string? loaiPhieu);
        Task<MauLuongKy> ChiTietAsync(int id);
        Task<MauLuongKy> ThemAsync(MauLuongKyRequest request);
        Task<MauLuongKy> SuaAsync(int id, MauLuongKyRequest request);
        Task XoaAsync(int id);
    }
}
