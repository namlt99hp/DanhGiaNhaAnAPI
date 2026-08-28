using DanhGiaAPI.DTOs.Auth;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST api/auth/dang-ky
        [HttpPost("dang-ky")]
        public async Task<IActionResult> DangKy([FromBody] DangKyRequest request)
        {
            try
            {
                await _authService.DangKyAsync(request);
                return Ok(new { message = "Đăng ký thành công, vui lòng chờ duyệt tài khoản." });
            }
            catch (AuthException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        // POST api/auth/dang-nhap
        [HttpPost("dang-nhap")]
        public async Task<ActionResult<DangNhapResponseDto>> DangNhap([FromBody] DangNhapRequest request)
        {
            try
            {
                var diaChiIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _authService.DangNhapAsync(request, diaChiIp);
                return Ok(result);
            }
            catch (AuthException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        // POST api/auth/dang-xuat
        [Authorize]
        [HttpPost("dang-xuat")]
        public async Task<IActionResult> DangXuat()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var token = authHeader.StartsWith("Bearer ") ? authHeader["Bearer ".Length..] : authHeader;
            await _authService.DangXuatAsync(token);
            return Ok(new { message = "Đăng xuất thành công." });
        }
    }
}
