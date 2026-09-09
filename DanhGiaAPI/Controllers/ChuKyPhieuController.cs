using DanhGiaAPI.DTOs.LuongKy;
using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    [Authorize]
    [Route("api/chu-ky-phieu")]
    [ApiController]
    public class ChuKyPhieuController : ControllerBase
    {
        private readonly IChuKyPhieuService _chuKyPhieuService;

        public ChuKyPhieuController(IChuKyPhieuService chuKyPhieuService)
        {
            _chuKyPhieuService = chuKyPhieuService;
        }

        // GET api/chu-ky-phieu?loaiDoiTuong=PHIEU1&doiTuongId=5 — tiến độ ký của 1 phiếu
        [HttpGet]
        public async Task<IActionResult> TienDo([FromQuery] string loaiDoiTuong, [FromQuery] int doiTuongId)
        {
            return Ok(await _chuKyPhieuService.TienDoKyAsync(loaiDoiTuong, doiTuongId));
        }

        // GET api/chu-ky-phieu/trang-thai?loaiDoiTuong=PHIEU1&doiTuongId=5
        [HttpGet("trang-thai")]
        public async Task<IActionResult> TrangThaiTong([FromQuery] string loaiDoiTuong, [FromQuery] int doiTuongId)
        {
            return Ok(new { trangThai = await _chuKyPhieuService.TrangThaiTongAsync(loaiDoiTuong, doiTuongId) });
        }

        // POST api/chu-ky-phieu/5/ky
        [HttpPost("{id}/ky")]
        public async Task<IActionResult> Ky(int id, [FromBody] KyPhieuRequest request)
        {
            return Ok(await _chuKyPhieuService.KyAsync(id, User.GetNguoiDungId(), request.ChuKyId, request.GhiChu, request.NguoiKyThayId));
        }

        // POST api/chu-ky-phieu/5/tu-choi
        [HttpPost("{id}/tu-choi")]
        public async Task<IActionResult> TuChoi(int id, [FromBody] TuChoiPhieuRequest request)
        {
            return Ok(await _chuKyPhieuService.TuChoiAsync(id, User.GetNguoiDungId(), request.GhiChu));
        }

        // PUT api/chu-ky-phieu/5/nguoi-ky-du-kien — chỉ định/đổi người ký dự
        // kiến cho 1 bước còn CHO_KY (xem 02. Phantich/huongdanquanlytaikhoan.md mục 5.1).
        [HttpPut("{id}/nguoi-ky-du-kien")]
        public async Task<IActionResult> DatNguoiKyDuKien(int id, [FromBody] DatNguoiKyDuKienRequest request)
        {
            return Ok(await _chuKyPhieuService.DatNguoiKyDuKienAsync(id, User.GetNguoiDungId(), request.NguoiKyDuKienId));
        }

        // GET api/chu-ky-phieu/5/nguoi-ky-kha-dung — danh sách người đủ điều
        // kiện ký bước này, dùng cho dropdown "chỉ định người ký"/"ký thay".
        [HttpGet("{id}/nguoi-ky-kha-dung")]
        public async Task<IActionResult> DanhSachNguoiKyKhaDung(int id)
        {
            return Ok(await _chuKyPhieuService.DanhSachNguoiKyKhaDungAsync(id));
        }
    }
}
