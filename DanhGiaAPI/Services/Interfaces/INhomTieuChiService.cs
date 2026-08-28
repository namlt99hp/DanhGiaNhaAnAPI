using DanhGiaAPI.DTOs.TieuChi;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface INhomTieuChiService
    {
        Task<List<NhomTieuChi>> DanhSachAsync(string? loaiPhieu, bool? dangHoatDong);
        Task<NhomTieuChi> ChiTietAsync(int id);
        Task<NhomTieuChi> ThemAsync(NhomTieuChiRequest request);
        Task<NhomTieuChi> SuaAsync(int id, NhomTieuChiRequest request);
        Task XoaAsync(int id);
    }
}
