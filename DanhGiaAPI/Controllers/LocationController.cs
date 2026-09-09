using DanhGiaAPI.DTOs.DiaDiemNhaAn;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // DiaDiemNhaAn = bảng "nhà ăn" (điểm ăn) có sẵn từ hệ thống chấm điểm bữa
    // ăn cũ, Phiếu 2/4 dùng lại làm danh mục Nhà ăn (xem
    // Repositories/Interfaces/IDiaDiemNhaAnRepository.cs). Route/tên controller
    // giữ nguyên "Location" để không phá vỡ FE đang gọi (services/diaDiemNhaAnApiV2.ts).
    // GET để public (không [Authorize]) vì được dùng làm danh mục chọn nhà ăn
    // khi lập phiếu — không phải dữ liệu nhạy cảm. Mutate yêu cầu policy
    // QuanLyDanhMuc, cùng nhóm với BepAn/NhaThau (xem 02. Phantich/modules/VaiTro.md).
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly IDiaDiemNhaAnService _diaDiemNhaAnService;

        public LocationController(IDiaDiemNhaAnService diaDiemNhaAnService)
        {
            _diaDiemNhaAnService = diaDiemNhaAnService;
        }

        // GET api/Location?isActive= (bỏ trống = lấy tất cả, kể cả ngừng hoạt động)
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool? isActive)
        {
            return Ok(await _diaDiemNhaAnService.DanhSachAsync(isActive));
        }

        // GET api/Location/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            return Ok(await _diaDiemNhaAnService.ChiTietAsync(id));
        }

        // POST api/Location
        [Authorize(Policy = "QuanLyDanhMuc")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DiaDiemNhaAnRequest request)
        {
            return Ok(await _diaDiemNhaAnService.ThemAsync(request));
        }

        // PUT api/Location/5
        [Authorize(Policy = "QuanLyDanhMuc")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] DiaDiemNhaAnRequest request)
        {
            return Ok(await _diaDiemNhaAnService.SuaAsync(id, request));
        }

        // DELETE api/Location/5
        [Authorize(Policy = "QuanLyDanhMuc")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _diaDiemNhaAnService.XoaAsync(id);
            return Ok(new { message = "Đã xóa địa điểm nhà ăn." });
        }
    }
}
