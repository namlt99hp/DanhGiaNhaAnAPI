using DanhGiaAPI.Models;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quartz;

namespace DanhGiaAPI.Jobs
{
    // Chạy nội bộ qua Quartz (1 trigger duy nhất, lịch cố định mỗi 6h — xem
    // appsettings "KetQuaDanhGiaPhanBoCron" + đăng ký ở Program.cs) — thay
    // cho cron ngoài trước đây, xem 02. Phantich/modules/KetQuaDanhGiaPhanBo.md.
    //
    // Mỗi lần job nổ: tính CATCH-UP cho TẤT CẢ lần kết thúc bữa ăn (so
    // KhungGioDanhGia.DenGio, ID khớp BuaAn.ID 1=Sáng..4=Đêm, cùng mapping với
    // DashboardController.ApplyBuaAnTimeFilter) rơi trong cửa sổ
    // [now - KetQuaDanhGiaPhanBoLookbackHours, now] — KHÔNG chỉ tính đúng 1
    // bữa gần nhất như trước (đổi 2026-10-19, xem lý do bên dưới).
    //
    // Lý do đổi: logic cũ chỉ tính "bữa gần now nhất tại đúng lúc job chạy",
    // không có trí nhớ gì về lần chạy trước -> job bỏ lỡ 1 lần fire (app
    // restart/deploy/crash đúng lúc đó, hoặc exception bị nuốt ở catch dưới)
    // là MẤT HẲN mọi bữa đã kết thúc trong lúc đó, không có cơ chế tự bắt lại
    // (lần chạy kế chỉ nhìn "bữa gần now nhất" của chính lúc nó chạy). Nay
    // quét lại TOÀN BỘ các lần DenGio đã qua trong cửa sổ lookback (mặc định
    // 48h — đủ bù 1 ngày downtime) mỗi lần job chạy: gọi lại
    // TinhLaiTheoNgayBuaAnAsync cho các (Ngay, IdBuaAn) đã tính rồi là AN
    // TOÀN và RẺ (service tự so ComThucTe/TongGoc, bỏ qua ghi đè nếu không
    // đổi — xem KetQuaDanhGiaPhanBoService.TinhLaiAsync), nên quét lại không
    // gây hại, chỉ tốn vài query đọc thêm.
    public class TinhLaiKetQuaDanhGiaPhanBoJob : IJob
    {
        private readonly AppDbContext _context;
        private readonly IKetQuaDanhGiaPhanBoService _service;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TinhLaiKetQuaDanhGiaPhanBoJob> _logger;

        public TinhLaiKetQuaDanhGiaPhanBoJob(
            AppDbContext context, IKetQuaDanhGiaPhanBoService service, IConfiguration configuration,
            ILogger<TinhLaiKetQuaDanhGiaPhanBoJob> logger)
        {
            _context = context;
            _service = service;
            _configuration = configuration;
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

                var lookbackHours = _configuration.GetValue<double?>("KetQuaDanhGiaPhanBoLookbackHours") ?? 48;
                var moc = now - TimeSpan.FromHours(lookbackHours);

                // Với mỗi khung giờ, liệt kê TẤT CẢ lần DenGio đã qua rơi trong
                // [moc, now] (không chỉ lần gần nhất) — lùi dần từng ngày từ lần
                // gần nhất cho tới khi ra ngoài cửa sổ lookback.
                var canTinh = new List<(int IdBuaAn, DateTime Ngay)>();
                foreach (var k in khungGio)
                {
                    var denGioHomNay = now.Date + k.DenGio;
                    var lan = denGioHomNay <= now ? denGioHomNay : denGioHomNay.AddDays(-1);
                    while (lan >= moc)
                    {
                        canTinh.Add((k.ID, lan.Date));
                        lan = lan.AddDays(-1);
                    }
                }

                foreach (var (idBuaAn, ngay) in canTinh.Distinct().OrderBy(x => x.Ngay).ThenBy(x => x.IdBuaAn))
                {
                    try
                    {
                        var soDong = await _service.TinhLaiTheoNgayBuaAnAsync(ngay, idBuaAn);
                        _logger.LogInformation(
                            "TinhLaiKetQuaDanhGiaPhanBoJob: idBuaAn={IdBuaAn} ngay={Ngay} soDongXuLy={SoDong}",
                            idBuaAn, ngay, soDong);
                    }
                    catch (Exception ex)
                    {
                        // Lỗi riêng 1 (Ngay, IdBuaAn) không được chặn các cặp còn
                        // lại trong cùng cửa sổ lookback.
                        _logger.LogError(ex, "TinhLaiKetQuaDanhGiaPhanBoJob lỗi idBuaAn={IdBuaAn} ngay={Ngay}", idBuaAn, ngay);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TinhLaiKetQuaDanhGiaPhanBoJob lỗi");
            }
        }
    }
}
