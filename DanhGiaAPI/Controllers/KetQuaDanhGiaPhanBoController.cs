using DanhGiaAPI.DTOs.KetQuaDanhGiaPhanBo;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Quyền RIÊNG QuanLyPhanBoDanhGia (tách khỏi QuanLyLichSuKhaoSat) để gán
    // độc lập cho vai trò ở màn Vai trò — xem
    // migration_quyen_ket_qua_danh_gia.sql. Áp cho CẢ controller (trước đây
    // GET dùng QuanLyLichSuKhaoSat, còn POST tinh-lai hoàn toàn KHÔNG có
    // [Authorize] — lỗ hổng cho phép gọi tay/backfill mà không cần quyền gì,
    // nay siết lại cùng 1 quyền cho cả 2 action).
    //
    // GET / — màn hình rà soát số liệu gốc vs đã phân bổ (xem
    // KetQuaDanhGiaPhanBoPageV2.tsx). Không truyền tuNgay/denNgay -> mặc định
    // 7 ngày gần nhất (kể cả hôm nay).
    //
    // POST /tinh-lai — tính lại theo từng bữa ăn. Từ khi thêm Quartz
    // (Jobs/TinhLaiKetQuaDanhGiaPhanBoJob.cs, lịch cấu hình ở appsettings
    // "KetQuaDanhGiaPhanBoCron"), job nội bộ tự gọi service này theo lịch;
    // endpoint dưới đây CHỈ còn dùng để gọi tay/backfill (VD tính lại 1 ngày
    // cụ thị do nghi ngờ sai số), không còn phải trigger từ cron ngoài nữa.
    // Xem 02. Phantich/modules/KetQuaDanhGiaPhanBo.md.
    [Authorize(Policy = "QuanLyPhanBoDanhGia")]
    [Route("api/[controller]")]
    [ApiController]
    public class KetQuaDanhGiaPhanBoController : ControllerBase
    {
        private readonly IKetQuaDanhGiaPhanBoService _service;
        public KetQuaDanhGiaPhanBoController(IKetQuaDanhGiaPhanBoService service)
        {
            _service = service;
        }

        // GET api/KetQuaDanhGiaPhanBo?diaDiemNhaAnId=&idBuaAn=&tuNgay=&denNgay=
        [HttpGet]
        public async Task<IActionResult> DanhSach(
            [FromQuery] int? diaDiemNhaAnId, [FromQuery] int? idBuaAn,
            [FromQuery] DateTime? tuNgay, [FromQuery] DateTime? denNgay)
        {
            var denNgayThat = (denNgay ?? DateTime.Now).Date;
            var tuNgayThat = (tuNgay ?? denNgayThat.AddDays(-6)).Date;
            return Ok(await _service.DanhSachAsync(diaDiemNhaAnId, idBuaAn, tuNgayThat, denNgayThat));
        }

        // POST api/KetQuaDanhGiaPhanBo/tinh-lai
        [HttpPost("tinh-lai")]
        public async Task<IActionResult> TinhLai([FromBody] KetQuaDanhGiaPhanBoTinhLaiRequest request)
        {
            var ngay = (request.Ngay ?? DateTime.Now).Date;
            var soDong = await _service.TinhLaiTheoNgayBuaAnAsync(ngay, request.ID_BuaAn);
            return Ok(new { soDongXuLy = soDong, ngay, idBuaAn = request.ID_BuaAn });
        }
    }
}
