using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Common;
using DanhGiaAPI.DTOs.Phieu3;
using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Dữ liệu nghiệp vụ nội bộ — toàn bộ action yêu cầu đăng nhập. Tài khoản
    // nhà thầu chỉ thấy báo cáo của chính nhà thầu đó — xem GetNhaThauId().
    [Authorize]
    [Route("api/phieu3")]
    [ApiController]
    public class Phieu3Controller : ControllerBase
    {
        private readonly IPhieu3Service _phieu3Service;

        public Phieu3Controller(IPhieu3Service phieu3Service)
        {
            _phieu3Service = phieu3Service;
        }

        // Phiếu 3 không có bước ký nào dành cho nhà thầu (chỉ xem + gửi ý
        // kiến phản hồi, xem huongdanquanlytaikhoan.md mục 5.1) — không được
        // TẠO phiếu mới.
        private void ChanTaoPhieuNhaThau()
        {
            if (User.GetNhaThauId().HasValue)
                throw new ApiException("Tài khoản nhà thầu không có quyền tạo phiếu mới", StatusCodes.Status403Forbidden);
        }

        // GET api/phieu3?nhaThauId=&thang=&nam=&trangThai=&tuNgay=&denNgay=&tuKhoa=&chiCuaToi=&page=&pageSize=
        [HttpGet]
        public async Task<IActionResult> DanhSach(
            [FromQuery] int? nhaThauId,
            [FromQuery] int? thang,
            [FromQuery] int? nam,
            [FromQuery] string? trangThai,
            [FromQuery] DateTime? tuNgay,
            [FromQuery] DateTime? denNgay,
            [FromQuery] string? tuKhoa,
            [FromQuery] bool chiCuaToi = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return Ok(await _phieu3Service.DanhSachAsync(
                nhaThauId, thang, nam, trangThai, tuNgay, denNgay, tuKhoa, chiCuaToi, page, pageSize,
                User.GetNhaThauId(), User.GetNguoiDungId(), User.GetLaAdmin()));
        }

        // GET api/phieu3/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            return Ok(await _phieu3Service.ChiTietAsync(id, User.GetNhaThauId(), User.GetNguoiDungId(), User.GetLaAdmin()));
        }

        // POST api/phieu3 — tạo phiếu + tự động tính Bảng 1 + khởi tạo khung Bảng 2
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] Phieu3Request request)
        {
            ChanTaoPhieuNhaThau();
            return Ok(await _phieu3Service.ThemAsync(request, User.GetNguoiDungId(), User.GetLaAdmin()));
        }

        // PUT api/phieu3/5 — lưu sửa tay Bảng 1 / Bảng 2
        [HttpPut("{id}")]
        public async Task<IActionResult> Sua(int id, [FromBody] Phieu3SuaRequest request)
        {
            return Ok(await _phieu3Service.SuaAsync(id, request, User.GetNguoiDungId()));
        }

        // POST api/phieu3/5/tinh-lai — tính lại Bảng 1 từ Phiếu 2 (giữ nguyên ô đã sửa tay)
        [HttpPost("{id}/tinh-lai")]
        public async Task<IActionResult> TinhLai(int id)
        {
            return Ok(await _phieu3Service.TinhLaiAsync(id));
        }

        // DELETE api/phieu3/5 — chỉ khi NHAP, trừ Admin (xóa được ở mọi trạng
        // thái, kèm dọn dẹp dữ liệu luồng ký liên quan)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _phieu3Service.XoaAsync(id, User.GetLaAdmin());
            return Ok(new { message = "Đã xóa báo cáo." });
        }

        // POST api/phieu3/5/gui-ky — NHAP/TU_CHOI -> CHO_KY
        [HttpPost("{id}/gui-ky")]
        public async Task<IActionResult> GuiKy(int id)
        {
            return Ok(await _phieu3Service.GuiKyAsync(id));
        }

        // POST api/phieu3/5/dong-bo-trang-thai
        [HttpPost("{id}/dong-bo-trang-thai")]
        public async Task<IActionResult> DongBoTrangThai(int id)
        {
            return Ok(await _phieu3Service.DongBoTrangThaiAsync(id));
        }

        // POST api/phieu3/5/y-kien-nha-thau
        [HttpPost("{id}/y-kien-nha-thau")]
        public async Task<IActionResult> PhanHoiYKienNhaThau(int id, [FromBody] Phieu3YKienNhaThauRequest request)
        {
            return Ok(await _phieu3Service.PhanHoiYKienNhaThauAsync(id, request));
        }

        // POST api/phieu3/5/doan — thêm 1 đoạn thời gian + địa điểm (chỉ
        // NHAP/TU_CHOI), tự tính lại Bảng 1 sau khi thêm
        [HttpPost("{id}/doan")]
        public async Task<IActionResult> ThemDoan(int id, [FromBody] DoanRequest request)
        {
            return Ok(await _phieu3Service.ThemDoanAsync(id, request));
        }

        // DELETE api/phieu3/5/doan/7
        [HttpDelete("{id}/doan/{doanId}")]
        public async Task<IActionResult> XoaDoan(int id, int doanId)
        {
            return Ok(await _phieu3Service.XoaDoanAsync(id, doanId));
        }
    }
}
