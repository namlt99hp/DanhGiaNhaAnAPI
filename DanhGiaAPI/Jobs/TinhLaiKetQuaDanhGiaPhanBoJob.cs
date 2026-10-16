using DanhGiaAPI.Models;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DanhGiaAPI.Jobs
{
    // Chạy nội bộ qua Quartz (1 trigger duy nhất, lịch cố định mỗi 6h — xem
    // appsettings "KetQuaDanhGiaPhanBoCron" + đăng ký ở Program.cs) — thay
    // cho cron ngoài trước đây, xem 02. Phantich/modules/KetQuaDanhGiaPhanBo.md.
    //
    // Mỗi lần job nổ: CHỈ tính cho bữa ăn GẦN NHẤT đã kết thúc (không lặp
    // qua cả 4 bữa). Xác định bằng cách so KhungGioDanhGia.DenGio (ID khớp
    // BuaAn.ID 1=Sáng..4=Đêm, cùng mapping với
    // DashboardController.ApplyBuaAnTimeFilter) với thời điểm hiện tại: với
    // mỗi khung giờ, lấy lần DenGio gần nhất ĐÃ QUA (DenGio hôm nay nếu đã
    // tới giờ, ngược lại DenGio hôm qua), rồi chọn khung có lần DenGio đó
    // gần hiện tại nhất — đó là bữa "gần nhất". Ngay dùng để tính = ngày của
    // đúng lần DenGio đã chọn (xử lý đúng ca Đêm kết thúc hôm qua nhưng job
    // chạy sau 0h hôm nay).
    public class TinhLaiKetQuaDanhGiaPhanBoJob : IJob
    {
        private readonly AppDbContext _context;
        private readonly IKetQuaDanhGiaPhanBoService _service;
        private readonly ILogger<TinhLaiKetQuaDanhGiaPhanBoJob> _logger;

        public TinhLaiKetQuaDanhGiaPhanBoJob(
            AppDbContext context, IKetQuaDanhGiaPhanBoService service, ILogger<TinhLaiKetQuaDanhGiaPhanBoJob> logger)
        {
            _context = context;
            _service = service;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // Bọc toàn bộ job trong try/catch: Quartz coi exception văng ra khỏi
            // Execute() là job "threw an unhandled Exception" (crash cả scheduler
            // thread của job đó), nên kể cả lỗi ở bước đọc KhungGioDanhGia (vd lỗi
            // transient SQL) cũng phải được nuốt + log ở đây thay vì để lọt ra ngoài.
            try
            {
                var now = DateTime.Now;
                var khungGio = await _context.KhungGioDanhGia.ToListAsync();
                if (khungGio.Count == 0) return;

                var buaAnGanNhat = khungGio
                    .Select(k =>
                    {
                        var denGioHomNay = now.Date + k.DenGio;
                        var lanGanNhat = denGioHomNay <= now ? denGioHomNay : denGioHomNay.AddDays(-1);
                        return (IdBuaAn: k.ID, LanDenGioGanNhat: lanGanNhat);
                    })
                    .OrderByDescending(x => x.LanDenGioGanNhat)
                    .First();

                var idBuaAn = buaAnGanNhat.IdBuaAn;
                var ngay = buaAnGanNhat.LanDenGioGanNhat.Date;

                var soDong = await _service.TinhLaiTheoNgayBuaAnAsync(ngay, idBuaAn);
                _logger.LogInformation(
                    "TinhLaiKetQuaDanhGiaPhanBoJob: bữa gần nhất idBuaAn={IdBuaAn} ngay={Ngay} soDongXuLy={SoDong}",
                    idBuaAn, ngay, soDong);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TinhLaiKetQuaDanhGiaPhanBoJob lỗi");
            }
        }
    }
}
