using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Phieu1;
using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Dữ liệu nghiệp vụ nội bộ (không phải danh mục) — toàn bộ action yêu cầu
    // đăng nhập, khác với GET công khai của BepAn/NhaThau/VaiTro/PhongBan.
    // Tài khoản nhà thầu chỉ thấy phiếu của chính nhà thầu đó (lọc theo claim
    // "nha_thau_id" — xem GetNhaThauId()); tài khoản nội bộ không bị lọc.
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

        // Nhà thầu chỉ ký/xem/xem tiến độ Phiếu 1 của chính mình — không được
        // TẠO phiếu mới (việc lập phiếu kiểm tra là của phòng ban nội bộ).
        private void ChanTaoPhieuNhaThau()
        {
            if (User.GetNhaThauId().HasValue)
                throw new ApiException("Tài khoản nhà thầu không có quyền tạo phiếu mới", StatusCodes.Status403Forbidden);
        }

        // GET api/phieu1?bepAnId=&phongBanId=&nhaThauId=&trangThai=&tuNgay=&denNgay=&tuKhoa=&chiCuaToi=&page=&pageSize=
        [HttpGet]
        public async Task<IActionResult> DanhSach(
            [FromQuery] int? bepAnId,
            [FromQuery] int? phongBanId,
            [FromQuery] int? nhaThauId,
            [FromQuery] string? trangThai,
            [FromQuery] DateTime? tuNgay,
            [FromQuery] DateTime? denNgay,
            [FromQuery] string? tuKhoa,
            [FromQuery] bool chiCuaToi = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return Ok(await _phieu1Service.DanhSachAsync(
                bepAnId, phongBanId, nhaThauId, trangThai, tuNgay, denNgay, tuKhoa, chiCuaToi, page, pageSize,
                User.GetNhaThauId(), User.GetNguoiDungId(), User.GetLaAdmin(), User.GetPhongBanId()));
        }

        // GET api/phieu1/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            return Ok(await _phieu1Service.ChiTietAsync(id, User.GetNhaThauId(), User.GetNguoiDungId(), User.GetLaAdmin(), User.GetPhongBanId()));
        }

        // POST api/phieu1
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] Phieu1Request request)
        {
            ChanTaoPhieuNhaThau();
            return Ok(await _phieu1Service.ThemAsync(request, User.GetNguoiDungId(), User.GetLaAdmin()));
        }

        // PUT api/phieu1/5 — chỉ khi phiếu đang NHAP hoặc TU_CHOI
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] Phieu1Request request)
        {
            return Ok(await _phieu1Service.SuaAsync(id, request));
        }

        // DELETE api/phieu1/5 — chỉ khi phiếu đang NHAP, trừ Admin (xóa được ở
        // mọi trạng thái, kèm dọn dẹp dữ liệu luồng ký liên quan)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _phieu1Service.XoaAsync(id, User.GetLaAdmin());
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
