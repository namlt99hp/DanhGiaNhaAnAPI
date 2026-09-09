using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Danh mục quyền cố định (mỗi Ma khớp 1 policy trong Program.cs) — chỉ đọc,
    // dùng cho màn hình cấu hình VaiTro. Không có API tạo/sửa/xóa vì thêm 1
    // quyền mới luôn cần thêm code (xem 02. Phantich/modules/VaiTro.md).
    [Authorize]
    [Route("api/quyen")]
    [ApiController]
    public class QuyenController : ControllerBase
    {
        private readonly IQuyenService _quyenService;

        public QuyenController(IQuyenService quyenService)
        {
            _quyenService = quyenService;
        }

        // GET api/quyen
        [HttpGet]
        public async Task<IActionResult> DanhSach()
        {
            return Ok(await _quyenService.DanhSachAsync());
        }
    }
}
