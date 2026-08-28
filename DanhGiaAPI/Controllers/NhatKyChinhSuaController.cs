using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Chỉ đọc — ghi log chỉ thực hiện nội bộ từ Service nghiệp vụ (Phiếu 3/4),
    // không có endpoint POST vì client không được tự tạo bản ghi audit.
    [Authorize]
    [Route("api/nhat-ky-chinh-sua")]
    [ApiController]
    public class NhatKyChinhSuaController : ControllerBase
    {
        private readonly INhatKyChinhSuaService _nhatKyChinhSuaService;

        public NhatKyChinhSuaController(INhatKyChinhSuaService nhatKyChinhSuaService)
        {
            _nhatKyChinhSuaService = nhatKyChinhSuaService;
        }

        // GET api/nhat-ky-chinh-sua?loaiDoiTuong=&doiTuongId=
        [HttpGet]
        public async Task<IActionResult> DanhSach([FromQuery] string loaiDoiTuong, [FromQuery] int doiTuongId)
        {
            return Ok(await _nhatKyChinhSuaService.LayTheoDoiTuongAsync(loaiDoiTuong, doiTuongId));
        }
    }
}
