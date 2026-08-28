using DanhGiaAPI.DTOs.NhaThau;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface INhaThauService
    {
        Task<List<NhaThau>> DanhSachAsync(string? trangThai);
        Task<NhaThau> ChiTietAsync(int id);
        Task<NhaThau> ThemAsync(NhaThauRequest request);
        Task<NhaThau> SuaAsync(int id, NhaThauRequest request);
        Task XoaAsync(int id);
    }
}
