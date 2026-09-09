using DanhGiaAPI.DTOs.LuongKy;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    [Route("api/mau-luong-ky")]
    [ApiController]
    public class MauLuongKyController : ControllerBase
    {
        private readonly IMauLuongKyService _mauLuongKyService;

        public MauLuongKyController(IMauLuongKyService mauLuongKyService)
        {
            _mauLuongKyService = mauLuongKyService;
        }

        // GET api/mau-luong-ky?loaiPhieu=
        [HttpGet]
        public async Task<IActionResult> DanhSach([FromQuery] string? loaiPhieu)
        {
            return Ok(await _mauLuongKyService.DanhSachAsync(loaiPhieu));
        }

        // GET api/mau-luong-ky/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            return Ok(await _mauLuongKyService.ChiTietAsync(id));
        }

        // POST api/mau-luong-ky
        [Authorize(Policy = "QuanLyLuongKy")]
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] MauLuongKyRequest request)
        {
            return Ok(await _mauLuongKyService.ThemAsync(request));
        }

        // PUT api/mau-luong-ky/5
        [Authorize(Policy = "QuanLyLuongKy")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] MauLuongKyRequest request)
        {
            return Ok(await _mauLuongKyService.SuaAsync(id, request));
        }

        // DELETE api/mau-luong-ky/5
        [Authorize(Policy = "QuanLyLuongKy")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _mauLuongKyService.XoaAsync(id);
            return Ok(new { message = "Đã xóa bước trong luồng ký." });
        }
    }
}
