using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    [Route("api/vai-tro")]
    [ApiController]
    public class VaiTroController : ControllerBase
    {
        private readonly IVaiTroService _vaiTroService;

        public VaiTroController(IVaiTroService vaiTroService)
        {
            _vaiTroService = vaiTroService;
        }

        // GET api/vai-tro
        [HttpGet]
        public async Task<IActionResult> DanhSach()
        {
            return Ok(await _vaiTroService.DanhSachAsync());
        }

        // POST api/vai-tro
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] VaiTroRequest request)
        {
            return Ok(await _vaiTroService.ThemAsync(request));
        }

        // PUT api/vai-tro/5
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] VaiTroRequest request)
        {
            return Ok(await _vaiTroService.SuaAsync(id, request));
        }

        // DELETE api/vai-tro/5
        [Authorize(Policy = "DuyetTaiKhoan")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _vaiTroService.XoaAsync(id);
            return Ok(new { message = "Đã xóa vai trò." });
        }
    }
}
