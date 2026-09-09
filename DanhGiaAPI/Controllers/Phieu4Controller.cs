using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Phieu4;
using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Dữ liệu nghiệp vụ nội bộ — toàn bộ action yêu cầu đăng nhập. Phiếu 4 là
    // ma trận NHIỀU nhà thầu, không có 1 nhà thầu sở hữu, và nghiệp vụ không
    // có bước nào nhà thầu tham gia xem/ký (chỉ P.ĐN/P.ATMT/BGĐ — xem
    // modules/LuongTrinhKy.md) -> tài khoản nhà thầu bị chặn hoàn toàn (403)
    // ở mọi action thay vì lọc bớt cột trong ma trận.
    [Authorize]
    [Route("api/phieu4")]
    [ApiController]
    public class Phieu4Controller : ControllerBase
    {
        private readonly IPhieu4Service _phieu4Service;

        public Phieu4Controller(IPhieu4Service phieu4Service)
        {
            _phieu4Service = phieu4Service;
        }

        private void ChanTaiKhoanNhaThau()
        {
            if (User.GetNhaThauId().HasValue)
                throw new ApiException("Tài khoản nhà thầu không có quyền truy cập Phiếu 4", StatusCodes.Status403Forbidden);
        }

        // GET api/phieu4?trangThai=
        [HttpGet]
        public async Task<IActionResult> DanhSach([FromQuery] string? trangThai)
        {
            ChanTaiKhoanNhaThau();
            return Ok(await _phieu4Service.DanhSachAsync(trangThai));
        }

        // GET api/phieu4/5
        [HttpGet("{id}")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            ChanTaiKhoanNhaThau();
            return Ok(await _phieu4Service.ChiTietAsync(id));
        }

        // POST api/phieu4 — tạo phiếu + chọn cột nhà thầu + tự động tính Bảng 1
        [HttpPost]
        public async Task<IActionResult> Them([FromBody] Phieu4Request request)
        {
            ChanTaiKhoanNhaThau();
            return Ok(await _phieu4Service.ThemAsync(request, User.GetNguoiDungId()));
        }

        // DELETE api/phieu4/5 — chỉ khi NHAP
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            ChanTaiKhoanNhaThau();
            await _phieu4Service.XoaAsync(id);
            return Ok(new { message = "Đã xóa phiếu tổng hợp." });
        }

        // POST api/phieu4/5/nha-thau — thêm 1 cột nhà thầu vào phiếu đã lập (chỉ khi NHAP/TU_CHOI)
        [HttpPost("{id}/nha-thau")]
        public async Task<IActionResult> ThemNhaThau(int id, [FromBody] Phieu4ThemNhaThauRequest request)
        {
            ChanTaiKhoanNhaThau();
            return Ok(await _phieu4Service.ThemNhaThauAsync(id, request.NhaThauId));
        }

        // DELETE api/phieu4/5/nha-thau/3 — xóa 1 cột nhà thầu khỏi phiếu đã lập (chỉ khi NHAP/TU_CHOI)
        [HttpDelete("{id}/nha-thau/{nhaThauId}")]
        public async Task<IActionResult> XoaNhaThau(int id, int nhaThauId)
        {
            ChanTaiKhoanNhaThau();
            return Ok(await _phieu4Service.XoaNhaThauAsync(id, nhaThauId));
        }

        // POST api/phieu4/5/tinh-lai — tính lại Bảng 1 từ Phiếu 2 (giữ nguyên ô đã sửa tay)
        [HttpPost("{id}/tinh-lai")]
        public async Task<IActionResult> TinhLai(int id)
        {
            ChanTaiKhoanNhaThau();
            return Ok(await _phieu4Service.TinhLaiAsync(id));
        }

        // PUT api/phieu4/5/gia-tri — sửa tay 1 hoặc nhiều ô (mọi bảng)
        [HttpPut("{id}/gia-tri")]
        public async Task<IActionResult> CapNhatGiaTri(int id, [FromBody] Phieu4CapNhatGiaTriRequest request)
        {
            ChanTaiKhoanNhaThau();
            return Ok(await _phieu4Service.CapNhatGiaTriAsync(id, request, User.GetNguoiDungId()));
        }

        // PUT api/phieu4/5/bang/7 — sửa tên bảng (Bảng 2-5 không còn API
        // thêm/sửa/xóa dòng — nội dung dòng cố định theo NhomTieuChi/TieuChi
        // master, tự đồng bộ mỗi lần GET chi tiết)
        [HttpPut("{id}/bang/{bangId}")]
        public async Task<IActionResult> SuaBang(int id, int bangId, [FromBody] Phieu4BangRequest request)
        {
            ChanTaiKhoanNhaThau();
            return Ok(await _phieu4Service.SuaBangAsync(id, bangId, request));
        }

        // POST api/phieu4/5/gui-ky — NHAP/TU_CHOI -> CHO_KY
        [HttpPost("{id}/gui-ky")]
        public async Task<IActionResult> GuiKy(int id)
        {
            ChanTaiKhoanNhaThau();
            return Ok(await _phieu4Service.GuiKyAsync(id));
        }

        // POST api/phieu4/5/dong-bo-trang-thai
        [HttpPost("{id}/dong-bo-trang-thai")]
        public async Task<IActionResult> DongBoTrangThai(int id)
        {
            ChanTaiKhoanNhaThau();
            return Ok(await _phieu4Service.DongBoTrangThaiAsync(id));
        }
    }
}
