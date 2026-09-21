using DanhGiaAPI.Extensions;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Đính kèm dùng chung cho Phiếu 1-4 — 2 luồng: ảnh rời (minh chứng, biết
    // ngay LoaiDoiTuong/DoiTuongId lúc upload) và ảnh nhúng CKEditor (chưa biết
    // dòng cha, chốt liên kết sau — xem ITepDinhKemService).
    // [Authorize]
    [Route("api/tep-dinh-kem")]
    [ApiController]
    public class TepDinhKemController : ControllerBase
    {
        private readonly ITepDinhKemService _tepDinhKemService;

        public TepDinhKemController(ITepDinhKemService tepDinhKemService)
        {
            _tepDinhKemService = tepDinhKemService;
        }

        // GET api/tep-dinh-kem?loaiDoiTuong=&doiTuongId=
        [HttpGet]
        public async Task<IActionResult> DanhSach([FromQuery] string? loaiDoiTuong, [FromQuery] int? doiTuongId)
        {
            return Ok(await _tepDinhKemService.DanhSachAsync(loaiDoiTuong, doiTuongId));
        }

        // POST api/tep-dinh-kem (multipart/form-data: file, loaiDoiTuong, doiTuongId?)
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string loaiDoiTuong, [FromForm] int? doiTuongId)
        {
            var tep = await _tepDinhKemService.UploadAsync(loaiDoiTuong, doiTuongId, file, User.GetNguoiDungId());
            return Ok(tep);
        }

        // POST api/tep-dinh-kem/ckeditor (multipart/form-data, field "upload" — theo chuẩn
        // CKEditor SimpleUploadAdapter). Trả về { url } đúng format adapter này yêu cầu.
        [HttpPost("ckeditor")]
        public async Task<IActionResult> UploadCkeditor(IFormFile upload)
        {
            var tep = await _tepDinhKemService.UploadCkeditorAsync(upload, User.GetNguoiDungId());

            // Trả về đường dẫn tương đối — FE tự ghép domain theo VITE_BASE_API của
            // nó (xem ApiRootV2 trong LinkServerV2.tsx), không dựa vào Request.Host ở
            // đây vì phía sau nginx sẽ không đáng tin cậy trừ khi nginx cấu hình
            // forward đúng header gốc.
            return Ok(new { url = tep.DuongDanTep });
        }

        // DELETE api/tep-dinh-kem/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Xoa(int id)
        {
            await _tepDinhKemService.XoaAsync(id);
            return Ok(new { message = "Đã xóa tệp đính kèm." });
        }
    }
}
