using System.Globalization;
using DanhGiaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Controllers
{
    // Endpoint tổng hợp số liệu cho DashboardPageV2 (thay cho báo cáo Power
    // BI cũ) — tính sẵn ở SQL vì KetQuaDanhGia/DuLieuCom có hàng trăm nghìn
    // tới hàng triệu dòng, không thể tải thô về FE rồi tự group.
    [Authorize(Policy = "QuanLyDashboard")]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // POST (không phải GET) vì filter có nhiều field chọn-nhiều
        // (List<T>) — gửi qua JSON body để FE không phải tự nối chuỗi/encode
        // query string cho array, và tránh đúng hạn chế model binding của
        // query string với List<T> (xem comment ở DashboardFilterParameters).
        [HttpPost("Summary")]
        public async Task<ActionResult<DashboardSummaryDTO>> GetSummary([FromBody] DashboardFilterParameters filter)
        {
            if (filter == null) return NotFound();

            var khungGio = await _context.KhungGioDanhGia.ToListAsync();

            var danhGiaQuery = _context.KetQuaDanhGia
                .Where(x => x.ThoiGianDanhGia >= filter.TuNgay.Date && x.ThoiGianDanhGia < filter.DenNgay.Date.AddDays(1));
            var comQuery = _context.DuLieuCom
                .Where(x => x.Ngay >= filter.TuNgay.Date && x.Ngay <= filter.DenNgay.Date);

            if (filter.DiaDiemIds != null && filter.DiaDiemIds.Count > 0)
            {
                danhGiaQuery = danhGiaQuery.Where(x => filter.DiaDiemIds.Contains(x.DiaDiem_ID));
                comQuery = comQuery.Where(x => filter.DiaDiemIds.Contains(x.ID_DiemAn));
            }

            // Tuần/Tháng đã chọn — mỗi nhóm là OR giữa các khoảng ngày trong
            // nhóm, lọc đồng thời trên cả danhGiaQuery (ThoiGianDanhGia) và
            // comQuery (Ngay) để "Tổng cơm thực tế"/"Tỷ lệ theo ngày" khớp
            // cùng khoảng đã chọn. Quy về danh sách ngày cụ thể rồi lọc bằng
            // List<DateTime>.Contains — EF Core dịch chắc chắn sang
            // "IN (...)" , tránh dựng tay cây Expression (đã gây lỗi SQL
            // "Conversion failed ... from character string" ở bản trước).
            if (filter.TuanDaChon != null && filter.TuanDaChon.Count > 0)
            {
                var ngayChoPhepTuan = DanhSachNgayTrongKhoang(filter.TuanDaChon.Select(ParseTuan));
                danhGiaQuery = danhGiaQuery.Where(x => ngayChoPhepTuan.Contains(x.ThoiGianDanhGia.Date));
                comQuery = comQuery.Where(x => ngayChoPhepTuan.Contains(x.Ngay.Date));
            }
            if (filter.ThangDaChon != null && filter.ThangDaChon.Count > 0)
            {
                var ngayChoPhepThang = DanhSachNgayTrongKhoang(filter.ThangDaChon.Select(ParseThang));
                danhGiaQuery = danhGiaQuery.Where(x => ngayChoPhepThang.Contains(x.ThoiGianDanhGia.Date));
                comQuery = comQuery.Where(x => ngayChoPhepThang.Contains(x.Ngay.Date));
            }

            danhGiaQuery = ApplyBuaAnTimeFilter(danhGiaQuery, filter.CodeBuaAnList, khungGio);

            var tongLuotDanhGia = await danhGiaQuery.CountAsync();
            var tongComThucTe = await SumComThucTeAsync(comQuery, filter.CodeBuaAnList);
            var tyLeDanhGiaPercent = tongComThucTe == 0 ? 0 : Math.Round(tongLuotDanhGia * 100.0 / tongComThucTe, 2);

            var theoMucRaw = await danhGiaQuery
                .GroupBy(x => x.DiemDanhGia)
                .Select(g => new { DiemDanhGia = g.Key, SoLuong = g.Count() })
                .ToListAsync();

            var theoMucDanhGia = new List<MucDanhGiaThongKeDTO>();
            for (int muc = 1; muc <= 5; muc++)
            {
                var soLuong = theoMucRaw.FirstOrDefault(x => x.DiemDanhGia == muc)?.SoLuong ?? 0;
                theoMucDanhGia.Add(new MucDanhGiaThongKeDTO
                {
                    DiemDanhGia = muc,
                    SoLuong = soLuong,
                    TyLePercent = tongLuotDanhGia == 0 ? 0 : Math.Round(soLuong * 100.0 / tongLuotDanhGia, 2)
                });
            }

            var danhGiaTheoNgayRaw = await danhGiaQuery
                .GroupBy(x => x.ThoiGianDanhGia.Date)
                .Select(g => new { Ngay = g.Key, SoLuong = g.Count() })
                .ToListAsync();
            var comTheoNgayRaw = await GroupComTheoNgayAsync(comQuery, filter.CodeBuaAnList);

            var tatCaNgay = danhGiaTheoNgayRaw.Select(x => x.Ngay)
                .Union(comTheoNgayRaw.Keys)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var theoNgay = new List<TyLeTheoNgayDTO>();
            foreach (var ngay in tatCaNgay)
            {
                var soLuotDanhGia = danhGiaTheoNgayRaw.FirstOrDefault(x => x.Ngay == ngay)?.SoLuong ?? 0;
                var soComThucTe = comTheoNgayRaw.TryGetValue(ngay, out var sc) ? sc : 0;
                theoNgay.Add(new TyLeTheoNgayDTO
                {
                    Ngay = ngay,
                    SoLuotDanhGia = soLuotDanhGia,
                    SoComThucTe = soComThucTe,
                    TyLePercent = soComThucTe == 0 ? 0 : Math.Round(soLuotDanhGia * 100.0 / soComThucTe, 2)
                });
            }

            var theoDiaDiemRaw = await danhGiaQuery
                .GroupBy(x => new { x.DiaDiem_ID, x.DiemDanhGia })
                .Select(g => new { g.Key.DiaDiem_ID, g.Key.DiemDanhGia, SoLuong = g.Count() })
                .ToListAsync();

            var diaDiemIdsCanLay = theoDiaDiemRaw.Select(x => x.DiaDiem_ID).Distinct().ToList();
            var tenDiaDiemMap = await _context.DiaDiemNhaAn
                .Where(x => diaDiemIdsCanLay.Contains(x.ID))
                .ToDictionaryAsync(x => x.ID, x => x.DiaDiem);

            var theoDiaDiem = diaDiemIdsCanLay.Select(id => new DiaDiemThongKeDTO
            {
                DiaDiemId = id,
                TenDiaDiem = tenDiaDiemMap.TryGetValue(id, out var ten) ? ten : "",
                Muc1 = theoDiaDiemRaw.FirstOrDefault(x => x.DiaDiem_ID == id && x.DiemDanhGia == 1)?.SoLuong ?? 0,
                Muc2 = theoDiaDiemRaw.FirstOrDefault(x => x.DiaDiem_ID == id && x.DiemDanhGia == 2)?.SoLuong ?? 0,
                Muc3 = theoDiaDiemRaw.FirstOrDefault(x => x.DiaDiem_ID == id && x.DiemDanhGia == 3)?.SoLuong ?? 0,
                Muc4 = theoDiaDiemRaw.FirstOrDefault(x => x.DiaDiem_ID == id && x.DiemDanhGia == 4)?.SoLuong ?? 0,
                Muc5 = theoDiaDiemRaw.FirstOrDefault(x => x.DiaDiem_ID == id && x.DiemDanhGia == 5)?.SoLuong ?? 0,
            })
            .OrderByDescending(x => x.Muc1 + x.Muc2 + x.Muc3 + x.Muc4 + x.Muc5)
            .ToList();

            return Ok(new DashboardSummaryDTO
            {
                TongLuotDanhGia = tongLuotDanhGia,
                TongComThucTe = tongComThucTe,
                TyLeDanhGiaPercent = tyLeDanhGiaPercent,
                TheoMucDanhGia = theoMucDanhGia,
                TheoNgay = theoNgay,
                TheoDiaDiem = theoDiaDiem
            });
        }

        // "{năm}-{số tuần ISO}" (vd "2026-44") -> khoảng Thứ 2 .. hết Chủ nhật
        // của tuần ISO đó. .NET có sẵn System.Globalization.ISOWeek (net6.0+)
        // nên không cần tự tính tay ngày đầu tuần.
        private static (DateTime Tu, DateTime Den) ParseTuan(string gia)
        {
            var phan = gia.Split('-');
            int nam = int.Parse(phan[0]);
            int tuan = int.Parse(phan[1]);
            var thuHai = ISOWeek.ToDateTime(nam, tuan, DayOfWeek.Monday).Date;
            var chuNhat = thuHai.AddDays(6);
            return (thuHai, chuNhat.AddDays(1).AddTicks(-1));
        }

        // "{năm}-{tháng 2 số}" (vd "2026-10") -> khoảng ngày 1 .. hết ngày
        // cuối tháng đó.
        private static (DateTime Tu, DateTime Den) ParseThang(string gia)
        {
            var phan = gia.Split('-');
            int nam = int.Parse(phan[0]);
            int thang = int.Parse(phan[1]);
            var dauThang = new DateTime(nam, thang, 1);
            return (dauThang, dauThang.AddMonths(1).AddTicks(-1));
        }

        // Quy mỗi khoảng (Tu, Den) thành danh sách từng ngày cụ thể (chỉ phần
        // ngày, bỏ giờ) để lọc bằng .Contains() — số ngày thực tế rất nhỏ
        // (vài tuần/tháng trong khoảng filter, tối đa vài trăm ngày), không
        // đáng lo về hiệu năng so với lợi ích dịch SQL chắc chắn.
        private static List<DateTime> DanhSachNgayTrongKhoang(IEnumerable<(DateTime Tu, DateTime Den)> khoangs)
        {
            var ds = new HashSet<DateTime>();
            foreach (var (tu, den) in khoangs)
            {
                for (var ngay = tu.Date; ngay <= den.Date; ngay = ngay.AddDays(1))
                {
                    ds.Add(ngay);
                }
            }
            return ds.ToList();
        }

        // OR giữa các khung giờ của các bữa ăn đã chọn (vd Sáng + Trưa) —
        // viết bằng lambda biên dịch thường (closure bắt các TimeSpan?
        // cụ thể), không dựng tay cây Expression (bản trước dựng tay bằng
        // System.Linq.Expressions gây lỗi SQL "Conversion failed ... from
        // character string" khi EF Core dịch sang SQL). Không chọn gì
        // (null/rỗng) thì không lọc theo bữa ăn.
        private static IQueryable<KetQuaDanhGia> ApplyBuaAnTimeFilter(IQueryable<KetQuaDanhGia> source, List<string>? codeBuaAnList, List<KhungGioDanhGia> khungGio)
        {
            if (codeBuaAnList == null || codeBuaAnList.Count == 0) return source;

            (TimeSpan Tu, TimeSpan Den)? LayKhung(string code, int id) =>
                codeBuaAnList.Contains(code) ? khungGio.Where(x => x.ID == id).Select(x => ((TimeSpan Tu, TimeSpan Den)?)(x.TuGio, x.DenGio)).FirstOrDefault() : null;

            var sang = LayKhung("01", 1);
            var trua = LayKhung("02", 2);
            var chieu = LayKhung("03", 3);
            var dem = LayKhung("04", 4);

            if (sang == null && trua == null && chieu == null && dem == null) return source;

            TimeSpan? sangTu = sang?.Tu, sangDen = sang?.Den;
            TimeSpan? truaTu = trua?.Tu, truaDen = trua?.Den;
            TimeSpan? chieuTu = chieu?.Tu, chieuDen = chieu?.Den;
            TimeSpan? demTu = dem?.Tu, demDen = dem?.Den;

            return source.Where(x =>
                (sangTu != null && x.ThoiGianDanhGia.TimeOfDay >= sangTu && x.ThoiGianDanhGia.TimeOfDay <= sangDen) ||
                (truaTu != null && x.ThoiGianDanhGia.TimeOfDay >= truaTu && x.ThoiGianDanhGia.TimeOfDay <= truaDen) ||
                (chieuTu != null && x.ThoiGianDanhGia.TimeOfDay >= chieuTu && x.ThoiGianDanhGia.TimeOfDay <= chieuDen) ||
                (demTu != null && x.ThoiGianDanhGia.TimeOfDay >= demTu && x.ThoiGianDanhGia.TimeOfDay <= demDen));
        }

        // Không chọn bữa ăn nào -> dùng cột tổng "_ALL" có sẵn. Chọn nhiều
        // bữa -> cộng các cột bữa tương ứng (Sang/Trua/Chieu/Dem) lại, không
        // dùng cột "_ALL" vì nó luôn là tổng cả 4 bữa, không phải tổng riêng
        // các bữa đã chọn.
        private static async Task<long> SumComThucTeAsync(IQueryable<DuLieuCom> source, List<string>? codeBuaAnList)
        {
            if (codeBuaAnList == null || codeBuaAnList.Count == 0)
            {
                return await source.SumAsync(x => (long)(x.Com_ThucTe_ALL ?? 0));
            }

            bool coSang = codeBuaAnList.Contains("01");
            bool coTrua = codeBuaAnList.Contains("02");
            bool coChieu = codeBuaAnList.Contains("03");
            bool coDem = codeBuaAnList.Contains("04");

            return await source.SumAsync(x =>
                (long)((coSang ? (x.Com_ThucTe_Sang ?? 0) : 0)
                     + (coTrua ? (x.Com_ThucTe_Trua ?? 0) : 0)
                     + (coChieu ? (x.Com_ThucTe_Chieu ?? 0) : 0)
                     + (coDem ? (x.Com_ThucTe_Dem ?? 0) : 0)));
        }

        private static async Task<Dictionary<DateTime, long>> GroupComTheoNgayAsync(IQueryable<DuLieuCom> source, List<string>? codeBuaAnList)
        {
            bool tatCa = codeBuaAnList == null || codeBuaAnList.Count == 0;
            bool coSang = !tatCa && codeBuaAnList!.Contains("01");
            bool coTrua = !tatCa && codeBuaAnList!.Contains("02");
            bool coChieu = !tatCa && codeBuaAnList!.Contains("03");
            bool coDem = !tatCa && codeBuaAnList!.Contains("04");

            return await source.GroupBy(x => x.Ngay.Date)
                .Select(g => new
                {
                    Ngay = g.Key,
                    Tong = g.Sum(x => tatCa
                        ? (long)(x.Com_ThucTe_ALL ?? 0)
                        : (long)((coSang ? (x.Com_ThucTe_Sang ?? 0) : 0)
                               + (coTrua ? (x.Com_ThucTe_Trua ?? 0) : 0)
                               + (coChieu ? (x.Com_ThucTe_Chieu ?? 0) : 0)
                               + (coDem ? (x.Com_ThucTe_Dem ?? 0) : 0)))
                })
                .ToDictionaryAsync(x => x.Ngay, x => x.Tong);
        }
    }
}
