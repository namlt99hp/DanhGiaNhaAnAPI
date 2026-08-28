using DanhGiaAPI.DTOs.Phieu1;
using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Dữ liệu nghiệp vụ nội bộ (không phải danh mục) — toàn bộ action yêu cầu
    // đăng nhập, khác với GET công khai của BepAn/NhaThau/VaiTro/PhongBan.
    // Lọc theo PhongBanId/NhaThauId của người dùng hiện tại (P.ĐN/P.ATMT/nhà
    // thầu chỉ thấy phiếu liên quan) CHƯA làm — xem DangNhap.md.
    [Authorize]
    [Route("api/phieu1")]
    [ApiController]
    public class Phieu1Controller : ControllerBase
    {
        private readonly IPhieu1Service _phieu1Service;

        public Phieu1Controller(IPhieu1Service phieu1Service)
        {
            _phieu1Service = phieu1Service;
        }

        // GET api/phieu1?bepAnId=&phongBanId=&nhaThauId=&trangThai=&tuNgay=&denNgay=
        [HttpGet]
        public async Task<IActionResult> DanhSach(
            [FromQuery] int? bepAnId,
            [FromQuery] int? phongBanId,
            [FromQuery] int? nhaThauId,
            [FromQuery] string? trangThai,
            [FromQuery] DateTime? tuNgay,
            [FromQuery] DateTime? denNgay)
        {
            return Ok(await _phieu1Service.DanhSachAsync(bepAnId, phongBanId, nhaThauId, trangThai, tuNgay, denNgay));
        }

        // GET api/phieu1/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            return Ok(await _phieu1Service.ChiTietAsync(id));
        }

        // POST api/phieu1
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] Phieu1Request request)
        {
            return Ok(await _phieu1Service.ThemAsync(request, User.GetNguoiDungId()));
        }

        // PUT api/phieu1/5 — chỉ khi phiếu đang NHAP hoặc TU_CHOI
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] Phieu1Request request)
        {
            return Ok(await _phieu1Service.SuaAsync(id, request));
        }

        // DELETE api/phieu1/5 — chỉ khi phiếu đang NHAP
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _phieu1Service.XoaAsync(id);
            return Ok(new { message = "Đã xóa phiếu kiểm tra." });
        }

        // POST api/phieu1/5/gui-ky — NHAP -> CHO_KY, khởi tạo luồng ký
        [HttpPost("{id}/gui-ky")]
        public async Task<IActionResult> GuiKy(int id)
        {
            return Ok(await _phieu1Service.GuiKyAsync(id));
        }

        // POST api/phieu1/5/dong-bo-trang-thai — gọi sau khi ký/từ chối thành
        // công qua /api/chu-ky-phieu/{id}/ky|tu-choi (xem LuongTrinhKy.md)
        [HttpPost("{id}/dong-bo-trang-thai")]
        public async Task<IActionResult> DongBoTrangThai(int id)
        {
            return Ok(await _phieu1Service.DongBoTrangThaiAsync(id));
        }
    }
}
