using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    [Route("api/phong-ban")]
    [ApiController]
    public class PhongBanController : ControllerBase
    {
        private readonly IPhongBanService _phongBanService;

        public PhongBanController(IPhongBanService phongBanService)
        {
            _phongBanService = phongBanService;
        }

        // GET api/phong-ban
        [HttpGet]
        public async Task<IActionResult> DanhSach([FromQuery] bool? dangHoatDong)
        {
            return Ok(await _phongBanService.DanhSachAsync(dangHoatDong));
        }

        // POST api/phong-ban
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] PhongBanRequest request)
        {
            return Ok(await _phongBanService.ThemAsync(request));
        }

        // PUT api/phong-ban/5
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] PhongBanRequest request)
        {
            return Ok(await _phongBanService.SuaAsync(id, request));
        }

        // DELETE api/phong-ban/5
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _phongBanService.XoaAsync(id);
            return Ok(new { message = "Đã xóa phòng ban." });
        }
    }
}
