using DanhGiaAPI.DTOs.DiaDiemNhaAn;
using DanhGiaAPI.Models;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IDiaDiemNhaAnService
    {
        Task<List<DiaDiemNhaAn>> DanhSachAsync(bool? isActive);
        Task<DiaDiemNhaAn> ChiTietAsync(int id);
        Task<DiaDiemNhaAn> ThemAsync(DiaDiemNhaAnRequest request);
        Task<DiaDiemNhaAn> SuaAsync(int id, DiaDiemNhaAnRequest request);
        Task XoaAsync(int id);
    }
}
