using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IChuKyService
    {
        Task<ChuKyResponseDto> UploadAsync(int nguoiDungId, IFormFile file);
        Task<List<ChuKyResponseDto>> DanhSachAsync(int nguoiDungId);
        Task KichHoatAsync(int nguoiDungId, int chuKyId);
    }
}
