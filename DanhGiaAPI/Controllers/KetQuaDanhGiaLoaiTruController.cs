using DanhGiaAPI.DTOs.KetQuaDanhGiaLoaiTru;
using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Đánh dấu (Ngày, Bữa ăn, Địa điểm) máy kiosk hỏng — loại trừ khỏi "Lượt
    // đánh giá" của Phiếu 3/4. Xem 02. Phantich/modules/KetQuaDanhGiaPhanBo.md,
    // mục "Loại trừ do lỗi thiết bị". Quyền RIÊNG QuanLyLoaiTruThietBi (tách
    // khỏi QuanLyDanhMuc) để gán độc lập cho vai trò ở màn Vai trò, xem
    // migration_quyen_ket_qua_danh_gia.sql.
    [Authorize(Policy = "QuanLyLoaiTruThietBi")]
    [Route("api/[controller]")]
    [ApiController]
    public class KetQuaDanhGiaLoaiTruController : ControllerBase
    {
        private readonly IKetQuaDanhGiaLoaiTruService _service;
        public KetQuaDanhGiaLoaiTruController(IKetQuaDanhGiaLoaiTruService service)
        {
            _service = service;
        }

        // GET api/KetQuaDanhGiaLoaiTru?diaDiemNhaAnId=&idBuaAn=&tuNgay=&denNgay=
        [HttpGet]
        public async Task<IActionResult> DanhSach(
            [FromQuery] int? diaDiemNhaAnId, [FromQuery] int? idBuaAn,
            [FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            return Ok(await _service.DanhSachAsync(diaDiemNhaAnId, idBuaAn, tuNgay, denNgay));
        }

        // POST api/KetQuaDanhGiaLoaiTru
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] KetQuaDanhGiaLoaiTruRequest request)
        {
            return Ok(await _service.ThemAsync(request, User.GetNguoiDungId()));
        }

        // DELETE api/KetQuaDanhGiaLoaiTru/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _service.XoaAsync(id);
            return Ok(new { message = "Đã hủy đánh dấu." });
        }
    }
}
