using DanhGiaAPI.DTOs.TieuChi;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface ITieuChiService
    {
        Task<List<TieuChi>> DanhSachAsync(int? nhomId, bool? dangHoatDong);
        Task<TieuChi> ChiTietAsync(int id);
        Task<TieuChi> ThemAsync(TieuChiRequest request);
        Task<TieuChi> SuaAsync(int id, TieuChiRequest request);
        Task XoaAsync(int id);
    }
}
