using DanhGiaAPI.DTOs.Auth;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task DangKyAsync(DangKyRequest request);
        Task<DangNhapResponseDto> DangNhapAsync(DangNhapRequest request, string? diaChiIp);
        Task DangXuatAsync(string token);
    }
}
