using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Toàn bộ endpoint quản lý tài khoản chỉ dành cho người có vai trò
    // VaiTro.CoQuyenDuyetTk = 1 (claim "co_quyen_duyet_tk" gắn lúc đăng nhập,
    // xem AuthService) — không hard-code riêng vai trò ADMIN.
    [Authorize(Policy = "DuyetTaiKhoan")]
    [Route("api/nguoi-dung")]
    [ApiController]
    public class NguoiDungController : ControllerBase
    {
        private readonly INguoiDungService _nguoiDungService;
        private readonly IChuKyService _chuKyService;

        public NguoiDungController(INguoiDungService nguoiDungService, IChuKyService chuKyService)
        {
            _nguoiDungService = nguoiDungService;
            _chuKyService = chuKyService;
        }

        // GET api/nguoi-dung?trangThai=&phongBanId=&nhaThauId=
        [HttpGet]
        public async Task<ActionResult<List<NguoiDungListItemDto>>> DanhSach(
            [FromQuery] string? trangThai, [FromQuery] int? phongBanId, [FromQuery] int? nhaThauId)
        {
            return Ok(await _nguoiDungService.DanhSachAsync(trangThai, phongBanId, nhaThauId));
        }

        // GET api/nguoi-dung/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NguoiDungListItemDto>> ChiTiet(int id)
        {
            return Ok(await _nguoiDungService.ChiTietAsync(id));
        }

        // POST api/nguoi-dung — Admin tạo trực tiếp 1 tài khoản (vào thẳng
        // HOAT_DONG, không qua CHO_DUYET như tự đăng ký ở AuthController).
        [HttpPost]
        public async Task<ActionResult<NguoiDungListItemDto>> Tao([FromBody] TaoTaiKhoanRequest request)
        {
            return Ok(await _nguoiDungService.TaoTaiKhoanAsync(request, User.GetNguoiDungId()));
        }

        // GET api/nguoi-dung/5/chu-ky — Admin xem danh sách chữ ký của 1 tài
        // khoản bất kỳ (khác /api/chu-ky vốn chỉ cho xem chữ ký của chính mình).
        [HttpGet("{id}/chu-ky")]
        public async Task<IActionResult> DanhSachChuKy(int id)
        {
            return Ok(await _chuKyService.DanhSachAsync(id));
        }

        // POST api/nguoi-dung/5/chu-ky (multipart/form-data, field "file")
        [HttpPost("{id}/chu-ky")]
        public async Task<IActionResult> UploadChuKy(int id, IFormFile file)
        {
            return Ok(await _chuKyService.UploadAsync(id, file));
        }

        // POST api/nguoi-dung/5/chu-ky/9/kich-hoat
        [HttpPost("{id}/chu-ky/{chuKyId}/kich-hoat")]
        public async Task<IActionResult> KichHoatChuKy(int id, int chuKyId)
        {
            await _chuKyService.KichHoatAsync(id, chuKyId);
            return Ok(new { message = "Đã đặt làm chữ ký hiện hành." });
        }

        // POST api/nguoi-dung/5/duyet
        [HttpPost("{id}/duyet")]
        public async Task<IActionResult> Duyet(int id)
        {
            await _nguoiDungService.DuyetAsync(id, User.GetNguoiDungId());
            return Ok(new { message = "Duyệt tài khoản thành công." });
        }

        // POST api/nguoi-dung/5/tu-choi
        [HttpPost("{id}/tu-choi")]
        public async Task<IActionResult> TuChoi(int id)
        {
            await _nguoiDungService.TuChoiAsync(id);
            return Ok(new { message = "Đã từ chối đăng ký." });
        }

        // POST api/nguoi-dung/5/khoa
        [HttpPost("{id}/khoa")]
        public async Task<IActionResult> Khoa(int id)
        {
            await _nguoiDungService.KhoaAsync(id);
            return Ok(new { message = "Đã khóa tài khoản." });
        }

        // POST api/nguoi-dung/5/mo-khoa
        [HttpPost("{id}/mo-khoa")]
        public async Task<IActionResult> MoKhoa(int id)
        {
            await _nguoiDungService.MoKhoaAsync(id);
            return Ok(new { message = "Đã mở khóa tài khoản." });
        }

        // PUT api/nguoi-dung/5/vai-tro
        [HttpPut("{id}/vai-tro")]
        public async Task<IActionResult> CapNhatVaiTro(int id, [FromBody] CapNhatVaiTroRequest request)
        {
            await _nguoiDungService.CapNhatVaiTroAsync(id, request.VaiTroIds);
            return Ok(new { message = "Cập nhật vai trò thành công." });
        }

        // POST api/nguoi-dung/5/reset-mat-khau — Admin đặt lại mật khẩu tài
        // khoản khác về mặc định "HPDQ@1234".
        [HttpPost("{id}/reset-mat-khau")]
        public async Task<IActionResult> ResetMatKhau(int id)
        {
            await _nguoiDungService.ResetMatKhauAsync(id);
            return Ok(new { message = "Đã đặt lại mật khẩu về mặc định." });
        }
    }
}
