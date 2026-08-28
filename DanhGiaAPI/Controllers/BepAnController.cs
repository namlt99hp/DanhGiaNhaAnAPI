using DanhGiaAPI.DTOs.BepAn;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    [Route("api/bep-an")]
    [ApiController]
    public class BepAnController : ControllerBase
    {
        private readonly IBepAnService _bepAnService;

        public BepAnController(IBepAnService bepAnService)
        {
            _bepAnService = bepAnService;
        }

        // GET api/bep-an?nhaThauId=&trangThai=
        [HttpGet]
        public async Task<IActionResult> DanhSach([FromQuery] int? nhaThauId, [FromQuery] string? trangThai)
        {
            return Ok(await _bepAnService.DanhSachAsync(nhaThauId, trangThai));
        }

        // GET api/bep-an/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            return Ok(await _bepAnService.ChiTietAsync(id));
        }

        // POST api/bep-an
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] BepAnRequest request)
        {
            return Ok(await _bepAnService.ThemAsync(request));
        }

        // PUT api/bep-an/5
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] BepAnRequest request)
        {
            return Ok(await _bepAnService.SuaAsync(id, request));
        }

        // DELETE api/bep-an/5
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _bepAnService.XoaAsync(id);
            return Ok(new { message = "Đã xóa bếp ăn." });
        }
    }
}
