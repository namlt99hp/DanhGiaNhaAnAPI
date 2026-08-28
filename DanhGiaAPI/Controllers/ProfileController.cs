using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Mỗi người dùng tự quản lý thông tin/mật khẩu của chính mình —
    // NguoiDungId luôn lấy từ claim JWT, không nhận từ client. Khác
    // NguoiDungController (yêu cầu policy "DuyetTaiKhoan"): endpoint ở đây
    // chỉ cần đã đăng nhập, không phân biệt vai trò.
    [Authorize]
    [Route("api/profile")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly INguoiDungService _nguoiDungService;

        public ProfileController(INguoiDungService nguoiDungService)
        {
            _nguoiDungService = nguoiDungService;
        }

        // GET api/profile
        [HttpGet]
        public async Task<IActionResult> ChiTiet()
        {
            return Ok(await _nguoiDungService.ChiTietAsync(User.GetNguoiDungId()));
        }

        // PUT api/profile
        [HttpPut]
        public async Task<IActionResult> CapNhat([FromBody] CapNhatProfileRequest request)
        {
            return Ok(await _nguoiDungService.CapNhatProfileAsync(User.GetNguoiDungId(), request));
        }

        // POST api/profile/doi-mat-khau
        [HttpPost("doi-mat-khau")]
        public async Task<IActionResult> DoiMatKhau([FromBody] DoiMatKhauRequest request)
        {
            await _nguoiDungService.DoiMatKhauAsync(User.GetNguoiDungId(), request);
            return Ok(new { message = "Đổi mật khẩu thành công." });
        }
    }
}
