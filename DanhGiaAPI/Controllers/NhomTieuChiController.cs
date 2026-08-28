using DanhGiaAPI.DTOs.TieuChi;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    [Route("api/nhom-tieu-chi")]
    [ApiController]
    public class NhomTieuChiController : ControllerBase
    {
        private readonly INhomTieuChiService _nhomTieuChiService;

        public NhomTieuChiController(INhomTieuChiService nhomTieuChiService)
        {
            _nhomTieuChiService = nhomTieuChiService;
        }

        // GET api/nhom-tieu-chi?loaiPhieu=&dangHoatDong=
        [HttpGet]
        public async Task<IActionResult> DanhSach([FromQuery] string? loaiPhieu, [FromQuery] bool? dangHoatDong)
        {
            return Ok(await _nhomTieuChiService.DanhSachAsync(loaiPhieu, dangHoatDong));
        }

        // GET api/nhom-tieu-chi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            return Ok(await _nhomTieuChiService.ChiTietAsync(id));
        }

        // POST api/nhom-tieu-chi
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] NhomTieuChiRequest request)
        {
            return Ok(await _nhomTieuChiService.ThemAsync(request));
        }

        // PUT api/nhom-tieu-chi/5
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] NhomTieuChiRequest request)
        {
            return Ok(await _nhomTieuChiService.SuaAsync(id, request));
        }

        // DELETE api/nhom-tieu-chi/5
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _nhomTieuChiService.XoaAsync(id);
            return Ok(new { message = "Đã xóa nhóm tiêu chí." });
        }
    }
}
