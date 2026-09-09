using DanhGiaAPI.DTOs.NhaThau;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    [Route("api/nha-thau")]
    [ApiController]
    public class NhaThauController : ControllerBase
    {
        private readonly INhaThauService _nhaThauService;

        public NhaThauController(INhaThauService nhaThauService)
        {
            _nhaThauService = nhaThauService;
        }

        // GET api/nha-thau?trangThai=
        [HttpGet]
        public async Task<IActionResult> DanhSach([FromQuery] string? trangThai)
        {
            return Ok(await _nhaThauService.DanhSachAsync(trangThai));
        }

        // GET api/nha-thau/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            return Ok(await _nhaThauService.ChiTietAsync(id));
        }

        // POST api/nha-thau
        [Authorize(Policy = "QuanLyDanhMuc")]
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] NhaThauRequest request)
        {
            return Ok(await _nhaThauService.ThemAsync(request));
        }

        // PUT api/nha-thau/5
        [Authorize(Policy = "QuanLyDanhMuc")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] NhaThauRequest request)
        {
            return Ok(await _nhaThauService.SuaAsync(id, request));
        }

        // DELETE api/nha-thau/5
        [Authorize(Policy = "QuanLyDanhMuc")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _nhaThauService.XoaAsync(id);
            return Ok(new { message = "Đã xóa nhà thầu." });
        }
    }
}
