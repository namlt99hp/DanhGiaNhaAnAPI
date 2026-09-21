using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Danh mục "Bữa ăn" (Sáng/Trưa/Chiều/Đêm, mã cố định 01-04) — 4 dòng
    // tĩnh, không có màn hình quản lý riêng. FE dùng để chọn bữa ăn bắt
    // đầu/kết thúc khi khai báo "đoạn" thời gian (Phiếu 3/4).
    [Route("api/bua-an")]
    [ApiController]
    public class BuaAnController : ControllerBase
    {
        private readonly IBuaAnRepository _buaAnRepository;

        public BuaAnController(IBuaAnRepository buaAnRepository)
        {
            _buaAnRepository = buaAnRepository;
        }

        // GET api/bua-an
        [HttpGet]
        public async Task<IActionResult> DanhSach()
        {
            return Ok(await _buaAnRepository.GetAllAsync());
        }
    }
}
