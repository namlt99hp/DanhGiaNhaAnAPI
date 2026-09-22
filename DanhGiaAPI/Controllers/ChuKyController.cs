using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Mỗi người dùng tự quản lý chữ ký của chính mình — NguoiDungId luôn lấy
    // từ claim JWT, không nhận từ client để tránh sửa/xem chữ ký người khác.
    [Authorize]
    [Route("api/chu-ky")]
    [ApiController]
    public class ChuKyController : ControllerBase
    {
        private readonly IChuKyService _chuKyService;

        public ChuKyController(IChuKyService chuKyService)
        {
            _chuKyService = chuKyService;
        }

        // GET api/chu-ky
        [HttpGet]
        public async Task<IActionResult> DanhSach()
        {
            return Ok(await _chuKyService.DanhSachAsync(User.GetNguoiDungId()));
        }

        // POST api/chu-ky (multipart/form-data, field "file")
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            return Ok(await _chuKyService.UploadAsync(User.GetNguoiDungId(), file));
        }

        // POST api/chu-ky/5/kich-hoat
        [HttpPost("{id}/kich-hoat")]
        public async Task<IActionResult> KichHoat(int id)
        {
            await _chuKyService.KichHoatAsync(User.GetNguoiDungId(), id);
            return Ok(new { message = "Đã đặt làm chữ ký hiện hành." });
        }

        // DELETE api/chu-ky/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _chuKyService.XoaAsync(User.GetNguoiDungId(), id);
            return Ok(new { message = "Đã xóa chữ ký." });
        }
    }
}
