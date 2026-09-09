using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Phieu2;
using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Dữ liệu nghiệp vụ nội bộ — toàn bộ action yêu cầu đăng nhập. Tài khoản
    // nhà thầu chỉ thấy phiếu của chính nhà thầu đó — xem GetNhaThauId().
    [Authorize]
    [Route("api/phieu2")]
    [ApiController]
    public class Phieu2Controller : ControllerBase
    {
        private readonly IPhieu2Service _phieu2Service;

        public Phieu2Controller(IPhieu2Service phieu2Service)
        {
            _phieu2Service = phieu2Service;
        }

        // Nhà thầu chỉ ký/xem/gửi ý kiến phản hồi Phiếu 2 của chính mình —
        // không được TẠO phiếu mới (việc lập phiếu đánh giá là của phòng ban
        // nội bộ, nhà thầu chỉ ký bước 1 sau khi phiếu đã được lập).
        private void ChanTaoPhieuNhaThau()
        {
            if (User.GetNhaThauId().HasValue)
                throw new ApiException("Tài khoản nhà thầu không có quyền tạo phiếu mới", StatusCodes.Status403Forbidden);
        }

        // GET api/phieu2?nhaThauId=&bepAnId=&thang=&nam=&trangThai=
        [HttpGet]
        public async Task<IActionResult> DanhSach(
            [FromQuery] int? nhaThauId,
            [FromQuery] int? bepAnId,
            [FromQuery] int? thang,
            [FromQuery] int? nam,
            [FromQuery] string? trangThai)
        {
            return Ok(await _phieu2Service.DanhSachAsync(nhaThauId, bepAnId, thang, nam, trangThai, User.GetNhaThauId()));
        }

        // GET api/phieu2/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            return Ok(await _phieu2Service.ChiTietAsync(id, User.GetNhaThauId()));
        }

        // GET api/phieu2/phieu1-kha-dung?nhaThauId=&bepAnId=
        [HttpGet("phieu1-kha-dung")]
        public async Task<IActionResult> DanhSachPhieu1KhaDung([FromQuery] int nhaThauId, [FromQuery] int? bepAnId)
        {
            return Ok(await _phieu2Service.DanhSachPhieu1KhaDungAsync(nhaThauId, bepAnId));
        }

        // POST api/phieu2
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] Phieu2Request request)
        {
            ChanTaoPhieuNhaThau();
            return Ok(await _phieu2Service.ThemAsync(request, User.GetNguoiDungId(), User.GetLaAdmin()));
        }

        // PUT api/phieu2/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] Phieu2Request request)
        {
            return Ok(await _phieu2Service.SuaAsync(id, request));
        }

        // DELETE api/phieu2/5 — chỉ khi NHAP
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _phieu2Service.XoaAsync(id);
            return Ok(new { message = "Đã xóa phiếu đánh giá." });
        }

        // POST api/phieu2/5/gui-ky — NHAP/TU_CHOI -> CHO_KY
        [HttpPost("{id}/gui-ky")]
        public async Task<IActionResult> GuiKy(int id)
        {
            return Ok(await _phieu2Service.GuiKyAsync(id));
        }

        // POST api/phieu2/5/dong-bo-trang-thai — gọi sau khi ký/từ chối qua
        // /api/chu-ky-phieu/{id}/ky|tu-choi (xem LuongTrinhKy.md)
        [HttpPost("{id}/dong-bo-trang-thai")]
        public async Task<IActionResult> DongBoTrangThai(int id)
        {
            return Ok(await _phieu2Service.DongBoTrangThaiAsync(id));
        }

        // POST api/phieu2/5/y-kien-nha-thau — nhà thầu nhập phản hồi
        [HttpPost("{id}/y-kien-nha-thau")]
        public async Task<IActionResult> PhanHoiYKienNhaThau(int id, [FromBody] Phieu2YKienNhaThauRequest request)
        {
            return Ok(await _phieu2Service.PhanHoiYKienNhaThauAsync(id, request));
        }
    }
}
