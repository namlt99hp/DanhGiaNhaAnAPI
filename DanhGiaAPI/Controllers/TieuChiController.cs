using DanhGiaAPI.DTOs.TieuChi;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    [Route("api/tieu-chi")]
    [ApiController]
    public class TieuChiController : ControllerBase
    {
        private readonly ITieuChiService _tieuChiService;

        public TieuChiController(ITieuChiService tieuChiService)
        {
            _tieuChiService = tieuChiService;
        }

        // GET api/tieu-chi?nhomId=&dangHoatDong=
        [HttpGet]
        public async Task<IActionResult> DanhSach([FromQuery] int? nhomId, [FromQuery] bool? dangHoatDong)
        {
            return Ok(await _tieuChiService.DanhSachAsync(nhomId, dangHoatDong));
        }

        // GET api/tieu-chi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            return Ok(await _tieuChiService.ChiTietAsync(id));
        }

        // POST api/tieu-chi
        [Authorize(Policy = "QuanLyTieuChi")]
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] TieuChiRequest request)
        {
            return Ok(await _tieuChiService.ThemAsync(request));
        }

        // PUT api/tieu-chi/5
        [Authorize(Policy = "QuanLyTieuChi")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] TieuChiRequest request)
        {
            return Ok(await _tieuChiService.SuaAsync(id, request));
        }

        // DELETE api/tieu-chi/5
        [Authorize(Policy = "QuanLyTieuChi")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _tieuChiService.XoaAsync(id);
            return Ok(new { message = "Đã xóa tiêu chí." });
        }
    }
}
