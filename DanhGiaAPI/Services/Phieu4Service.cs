using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Common;
using DanhGiaAPI.DTOs.Phieu4;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    // Xem quyết định thiết kế + các giả định nghiệp vụ đã xác nhận tại
    // 02. Phantich/modules/Phieu4_TongHopPhanBo.md.
    //
    // ĐỔI LỚN (xem 02. Phantich/migration_phieu3_phieu4_doan_thoi_gian.sql):
    // Bảng 1 KHÔNG còn suy luận "địa điểm rõ ràng" từ Phiếu 2 trên 1 khoảng
    // ngày liên tục [TuNgay,DenNgay] duy nhất của cả phiếu nữa — 1 nhà thầu
    // có thể đổi tập địa điểm phụ trách GIỮA kỳ báo cáo, suy luận theo khối
    // ngày duy nhất tính sai ở ranh giới đổi địa điểm. Thay bằng khai báo
    // TƯỜNG MINH: mỗi CỘT nhà thầu (Phieu4_NhaThau) có 1 bộ "đoạn" thời gian
    // riêng (Phieu4_Doan + Phieu4_DoanDiaDiem — khung (Ngày,Bữa ăn) bắt đầu ->
    // (Ngày,Bữa ăn) kết thúc, CẢ 2 ĐẦU bao gồm bữa được chọn, + danh sách địa
    // điểm áp dụng), tương đương mỗi cột là 1 "Phiếu 3 con". Xem
    // ThemDoanAsync/XoaDoanAsync + các hàm *TuDoanAsync bên dưới.
    //
    // Bảng 1 (cấu trúc CỐ ĐỊNH — 4 nhóm dòng, giống mục 5.8 PhanTichNghiepVu.md):
    //   Nhóm 1 (1 dòng "Tổng số suất ăn"): TỰ ĐỘNG = tổng DuLieuCom.Com_ThucTe_
    //     {Sang,Trua,Chieu,Dem} theo đúng biên bữa ăn của từng đoạn/địa điểm đã
    //     khai báo cho cột nhà thầu đó — xem TinhTongSuatAnTuDoanAsync.
    //   Nhóm 2 (5 dòng "Số lượt đánh giá mức 1..5"): TỰ ĐỘNG — đếm
    //     KetQuaDanhGia (hệ kiosk CBNV tự chấm 1-5, đọc thẳng ID_BuaAn — đã
    //     được gán đáng tin cậy lúc kiosk ghi, xem EvaluatesController.Post)
    //     tại các địa điểm/đoạn thời gian đã khai báo — xem
    //     TinhSoLuotCbnvTheoMucTuDoanAsync.
    //   Nhóm 3 (1 dòng "Điểm đánh giá trung bình"): TỰ ĐỘNG =
    //     (1*sl1+2*sl2+3*sl3+4*sl4+5*sl5) / tổng lượt (nhóm 2), cùng cột nhà thầu.
    //   Nhóm 4 (1 dòng "Tỷ lệ CBNV tham gia đánh giá"): TỰ ĐỘNG = tổng nhóm 2 /
    //     nhóm 1 × 100%, chỉ tính được khi nhóm 1 (Tổng số suất ăn) đã nhập > 0.
    //
    // Quy tắc "tính lại": chỉ ghi đè các ô Bảng 1 (nhóm 1/2/3/4) có
    // ChinhSuaThuCong = false — giữ nguyên ô người dùng đã tự sửa.
    //
    // Bảng 2: CỐ ĐỊNH hard-code (đổi từ master sang hard-code 2026-09-01, xem
    // TieuChiBang2/KhoiTaoBang2CoDinhAsync/TinhLaiBang2Async) — 12 dòng (2
    // phòng ban P.ĐN/P.ATMT × 6 tiêu chí), P.ĐN đa số TỰ ĐỘNG từ Phiếu 2,
    // P.ATMT chỉ VSATTP TỰ ĐỘNG từ Phiếu 1, "Đa dạng thực đơn" luôn nhập tay.
    // KHÔNG quan tâm địa điểm/bữa ăn — chỉ lọc Phiếu 2/Phiếu 1 theo NGÀY có
    // rơi vào UNION các đoạn của cột nhà thầu đó hay không (TrongDoanNao) —
    // công thức tính điểm giữ nguyên hoàn toàn.
    //
    // Phiếu 4.11.14: BỎ HẲN Bảng 3 ("Cơ chế hiệu chỉnh"/"Điểm tổng hợp"),
    // Bảng 4 ("Phân bổ theo Nhà ăn"), Bảng 5 ("Phân bổ theo Nhà thầu") khỏi
    // luồng tạo/tính lại — Phiếu 4 giờ CHỈ còn Bảng 1/2 (xác nhận nghiệp vụ).
    // Phiếu 4 tạo TRƯỚC thời điểm đổi vẫn còn dữ liệu Bảng 3/4/5 nguyên vẹn
    // trong DB (không xóa, không đồng bộ/tính lại nữa) — chỉ ngừng SINH MỚI.
    public class Phieu4Service : IPhieu4Service
    {
        private readonly IPhieu4TongHopRepository _phieuRepository;
        private readonly IPhieu4NhaThauRepository  _nhaThauCotRepository;
        private readonly IPhieu4BangRepository     _bangRepository;
        private readonly IPhieu4DongRepository     _dongRepository;
        private readonly IPhieu4GiaTriRepository   _giaTriRepository;
        private readonly IPhieu4DoanRepository     _doanRepository;
        private readonly IPhieu4DoanDiaDiemRepository _doanDiaDiemRepository;
        private readonly IBuaAnRepository          _buaAnRepository;
        private readonly IPhieu2DanhGiaRepository  _phieu2Repository;
        private readonly IPhieu2TieuChiRepository  _phieu2TieuChiRepository;
        private readonly IPhieu1KiemTraRepository  _phieu1Repository;
        private readonly IPhieu1KetLuanRepository  _phieu1KetLuanRepository;
        private readonly IKetQuaDanhGiaRepository  _ketQuaDanhGiaRepository;
        private readonly IDuLieuComRepository      _duLieuComRepository;
        private readonly INhaThauRepository        _nhaThauRepository;
        private readonly IPhongBanRepository       _phongBanRepository;
        private readonly INguoiDungPhieuQuyenRepository _nguoiDungPhieuQuyenRepository;
        private readonly IQuyenXemPhieuService     _quyenXemPhieuService;
        private readonly ISoHieuService            _soHieuService;
        private readonly IChuKyPhieuService         _chuKyPhieuService;
        private readonly IChuKyPhieuRepository       _chuKyPhieuRepository;
        private readonly INhatKyChinhSuaService     _nhatKyChinhSuaService;
        private readonly IUnitOfWork                _unitOfWork;

        // Bảng 2 — CỐ ĐỊNH (đổi từ master NhomTieuChi/TieuChi sang hard-code,
        // xác nhận nghiệp vụ 2026-09-01, giống Phieu3Service): 6 tiêu chí ×
        // 2 phòng ban (P.ĐN Stt 1-6, P.ATMT Stt 7-12 — dùng Stt liên tục toàn
        // bảng để 12 dòng không đụng nhau khi FE sort theo Stt). TC3 "Đa dạng
        // thực đơn" không có nguồn tự động ở nhánh nào -> luôn nhập tay.
        //
        // QUAN TRỌNG: thứ tự mảng này (theo Stt) PHẢI khớp CHÍNH XÁC với mảng
        // TIEU_CHI_BANG2 ở DanhGiaNhaAnUI/src/config/phieu4BangConfig.ts (FE
        // chỉ hard-code nhãn hiển thị, không đọc MaTieuChiPhieu2 ở đây) — đổi
        // thứ tự/thêm/bớt 1 tiêu chí ở bên nào thì PHẢI sửa bên kia theo đúng
        // cùng MaTieuChiPhieu2, nếu không nhãn cột sẽ lệch khỏi số liệu.
        private static readonly (int Stt, string Ten, string? MaTieuChiPhieu2)[] TieuChiBang2 = new[]
        {
            (1, "Tuân thủ đúng quy định về vệ sinh an toàn thực phẩm", "VSATTP"),
            (2, "Tuân thủ định lượng theo thực đơn đã được phê duyệt", "DINH_LUONG_THUC_DON"),
            (3, "Đa dạng thực đơn", (string?)null),
            (4, "Tuân thủ hợp đồng, bản cam kết, quy trình báo cáo", "DIEU_KHOAN_KHAC"),
            (5, "Thái độ phối hợp, cầu thị cải tiến", "THAI_DO_PHOI_HOP"),
            (6, "Phản hồi sự cố, xử lý khiếu nại nhanh chóng", "PHAN_HOI_SU_CO"),
        };

        public Phieu4Service(
            IPhieu4TongHopRepository phieuRepository,
            IPhieu4NhaThauRepository nhaThauCotRepository,
            IPhieu4BangRepository bangRepository,
            IPhieu4DongRepository dongRepository,
            IPhieu4GiaTriRepository giaTriRepository,
            IPhieu4DoanRepository doanRepository,
            IPhieu4DoanDiaDiemRepository doanDiaDiemRepository,
            IBuaAnRepository buaAnRepository,
            IPhieu2DanhGiaRepository phieu2Repository,
            IPhieu2TieuChiRepository phieu2TieuChiRepository,
            IPhieu1KiemTraRepository phieu1Repository,
            IPhieu1KetLuanRepository phieu1KetLuanRepository,
            IKetQuaDanhGiaRepository ketQuaDanhGiaRepository,
            IDuLieuComRepository duLieuComRepository,
            INhaThauRepository nhaThauRepository,
            IPhongBanRepository phongBanRepository,
            INguoiDungPhieuQuyenRepository nguoiDungPhieuQuyenRepository,
            IQuyenXemPhieuService quyenXemPhieuService,
            ISoHieuService soHieuService,
            IChuKyPhieuService chuKyPhieuService,
            IChuKyPhieuRepository chuKyPhieuRepository,
            INhatKyChinhSuaService nhatKyChinhSuaService,
            IUnitOfWork unitOfWork)
        {
            _phieuRepository        = phieuRepository;
            _nhaThauCotRepository   = nhaThauCotRepository;
            _bangRepository         = bangRepository;
            _dongRepository         = dongRepository;
            _giaTriRepository       = giaTriRepository;
            _doanRepository         = doanRepository;
            _doanDiaDiemRepository  = doanDiaDiemRepository;
            _buaAnRepository        = buaAnRepository;
            _phieu2Repository       = phieu2Repository;
            _phieu2TieuChiRepository = phieu2TieuChiRepository;
            _phieu1Repository       = phieu1Repository;
            _phieu1KetLuanRepository = phieu1KetLuanRepository;
            _ketQuaDanhGiaRepository = ketQuaDanhGiaRepository;
            _duLieuComRepository    = duLieuComRepository;
            _nhaThauRepository      = nhaThauRepository;
            _phongBanRepository     = phongBanRepository;
            _nguoiDungPhieuQuyenRepository = nguoiDungPhieuQuyenRepository;
            _quyenXemPhieuService   = quyenXemPhieuService;
            _soHieuService          = soHieuService;
            _chuKyPhieuService      = chuKyPhieuService;
            _chuKyPhieuRepository   = chuKyPhieuRepository;
            _nhatKyChinhSuaService  = nhatKyChinhSuaService;
            _unitOfWork             = unitOfWork;
        }

        // ============================================================
        // DANH SÁCH
        // ============================================================

        // Quyền "Đánh giá / nhập liệu" — cùng pattern Phieu1Service/Phieu2Service
        // (trước đây Phiếu 4 KHÔNG có gate nào, mọi tài khoản nội bộ đều tạo
        // được — xem VaiTro.md mục 10).
        private async Task KiemTraQuyenDanhGiaAsync(int? nguoiDungId, bool laAdmin)
        {
            if (laAdmin) return;

            if (!nguoiDungId.HasValue || !await _nguoiDungPhieuQuyenRepository.AnyAsync(
                    x => x.NguoiDungId == nguoiDungId.Value && x.LoaiPhieu == "PHIEU4" && x.DuocDanhGia))
                throw new ApiException(
                    "Bạn không có quyền tạo Phiếu 4 — liên hệ Admin để được phân quyền \"Đánh giá / nhập liệu\" ở mục Phân quyền theo Phiếu",
                    StatusCodes.Status403Forbidden);
        }

        // Quyền XEM (danh sách + chi tiết) của tài khoản NỘI BỘ — tài khoản
        // nhà thầu đã bị chặn hoàn toàn ở Controller (ChanTaiKhoanNhaThau),
        // không cần xử lý riêng ở đây.
        private async Task KiemTraQuyenXemAsync(int nguoiDungId, bool laAdmin)
        {
            if (laAdmin) return;

            if (!await _quyenXemPhieuService.CoQuyenXemAsync(nguoiDungId, "PHIEU4"))
                throw new ApiException(
                    "Bạn không có quyền truy cập Phiếu 4 — liên hệ Admin để được phân quyền ở mục Phân quyền theo Phiếu",
                    StatusCodes.Status403Forbidden);
        }

        public async Task<PagedResultDto<Phieu4TongHop>> DanhSachAsync(
            string? trangThai, DateTime? tuNgay, DateTime? denNgay, string? tuKhoa, bool chiCuaToi, int page, int pageSize,
            int nguoiDungId, bool laAdmin)
        {
            await KiemTraQuyenXemAsync(nguoiDungId, laAdmin);

            var query = _phieuRepository.Query();
            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.TrangThai == trangThai);
            if (tuNgay.HasValue) query = query.Where(x => x.TuNgay >= tuNgay.Value.Date);
            if (denNgay.HasValue) query = query.Where(x => x.TuNgay <= denNgay.Value.Date);
            if (!string.IsNullOrWhiteSpace(tuKhoa)) query = query.Where(x => x.SoHieu.Contains(tuKhoa));
            if (chiCuaToi) query = query.Where(x => x.NguoiTao == nguoiDungId);

            var tongSo = query.Count();
            var items = query.OrderByDescending(x => x.TuNgay).ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PagedResultDto<Phieu4TongHop> { Items = items, TotalCount = tongSo, Page = page, PageSize = pageSize };
        }

        // ============================================================
        // CHI TIẾT
        // ============================================================

        public async Task<Phieu4ResponseDto> ChiTietAsync(int id, int nguoiDungId, bool laAdmin)
        {
            await KiemTraQuyenXemAsync(nguoiDungId, laAdmin);
            return await LayChiTietAsync(id);
        }

        // Tách khỏi ChiTietAsync để các thao tác NỘI BỘ tự build lại response
        // DTO sau khi ghi mà không phải soi lại quyền XEM (xem Phieu1Service).
        private async Task<Phieu4ResponseDto> LayChiTietAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);

            var nhaThau = (await _nhaThauCotRepository.FindAsync(x => x.PhieuId == id))
                          .OrderBy(x => x.ThuTu).ToList();

            var bang = (await _bangRepository.FindAsync(x => x.PhieuId == id))
                       .OrderBy(x => x.SoBang).ToList();

            // Bảng 2: CỐ ĐỊNH (đổi từ master NhomTieuChi/TieuChi 2026-09-02,
            // xem doc comment đầu file) — đồng bộ dòng còn thiếu mỗi lần đọc
            // phiếu, không cần thao tác "thêm dòng" thủ công. Bảng 3/4/5
            // KHÔNG còn đồng bộ/tính lại nữa (đã bỏ khỏi Phiếu 4 mới) — phiếu
            // cũ tạo trước đây vẫn trả về dữ liệu Bảng 3/4/5 y nguyên (không
            // xóa) khi GET chi tiết, chỉ không tự tính lại/thêm dòng mới nữa.
            var nhaThauIdsHienTai = nhaThau.Select(x => x.NhaThauId).ToList();
            var bang2Entity = bang.FirstOrDefault(x => x.SoBang == 2);
            if (bang2Entity != null)
                await DongBoBang2CoDinhAsync(bang2Entity, nhaThauIdsHienTai);

            var bangIds = bang.Select(x => x.Id).ToList();

            var dong = (await _dongRepository.FindAsync(x => bangIds.Contains(x.BangId)))
                       .OrderBy(x => x.NhomSo).ThenBy(x => x.Stt).ToList();
            var dongIds = dong.Select(x => x.Id).ToList();

            var giaTri = await _giaTriRepository.FindAsync(x => dongIds.Contains(x.DongId));

            var bangDto = bang.Select(b => new Phieu4BangDto
            {
                Id = b.Id,
                SoBang = b.SoBang,
                TenBang = b.TenBang,
                Dong = dong.Where(d => d.BangId == b.Id).Select(d => new Phieu4DongDto
                {
                    Id = d.Id,
                    BangId = d.BangId,
                    NhomSo = d.NhomSo,
                    Stt = d.Stt,
                    NoiDung = d.NoiDung,
                    Dvt = d.Dvt,
                    LoaiDong = d.LoaiDong,
                    CongThuc = d.CongThuc,
                    TieuChiId = d.TieuChiId,
                    NhomTieuChiId = d.NhomTieuChiId,
                    DiaDiemNhaAnId = d.DiaDiemNhaAnId,
                    NhaThauId = d.NhaThauId,
                    GiaTriChung = d.GiaTriChung,
                    ChinhSuaThuCong = d.ChinhSuaThuCong,
                    GiaTri = giaTri.Where(g => g.DongId == d.Id).ToList(),
                }).ToList(),
            }).ToList();

            var nhaThauDto = await BuildNhaThauDtoAsync(nhaThau);

            return new Phieu4ResponseDto { Phieu = phieu, NhaThau = nhaThauDto, Bang = bangDto };
        }

        // Gắn kèm bộ "đoạn" thời gian/địa điểm CỦA RIÊNG từng cột nhà thầu —
        // thay cho trả thẳng entity Phieu4NhaThau thô.
        private async Task<List<Phieu4NhaThauDto>> BuildNhaThauDtoAsync(List<Phieu4NhaThau> nhaThau)
        {
            var cotIds = nhaThau.Select(x => x.Id).ToList();
            var doanEntities = cotIds.Count > 0
                ? await _doanRepository.FindAsync(x => cotIds.Contains(x.NhaThauCotId))
                : new List<Phieu4Doan>();
            var doanIds = doanEntities.Select(x => x.Id).ToList();
            var doanDiaDiem = doanIds.Count > 0
                ? await _doanDiaDiemRepository.FindAsync(x => doanIds.Contains(x.DoanId))
                : new List<Phieu4DoanDiaDiem>();
            var buaAnTheoId = (await _buaAnRepository.GetAllAsync()).ToDictionary(b => b.ID, b => b.CodeBuaAn);

            return nhaThau.Select(cot => new Phieu4NhaThauDto
            {
                Id = cot.Id,
                PhieuId = cot.PhieuId,
                NhaThauId = cot.NhaThauId,
                ThuTu = cot.ThuTu,
                Doan = doanEntities
                    .Where(d => d.NhaThauCotId == cot.Id)
                    .OrderBy(d => d.TuNgay).ThenBy(d => d.Id)
                    .Select(d => new DoanDto
                    {
                        Id = d.Id,
                        TuNgay = d.TuNgay,
                        TuBuaAnId = d.TuBuaAnId,
                        TuBuaAnCode = buaAnTheoId.TryGetValue(d.TuBuaAnId, out var tuMa) ? tuMa : null,
                        DenNgay = d.DenNgay,
                        DenBuaAnId = d.DenBuaAnId,
                        DenBuaAnCode = buaAnTheoId.TryGetValue(d.DenBuaAnId, out var denMa) ? denMa : null,
                        DiaDiemNhaAnIds = doanDiaDiem.Where(x => x.DoanId == d.Id).Select(x => x.DiaDiemNhaAnId).ToList(),
                    }).ToList(),
            }).ToList();
        }

        // ============================================================
        // TẠO MỚI
        // ============================================================

        public async Task<Phieu4ResponseDto> ThemAsync(Phieu4Request request, int? nguoiTaoId, bool laAdmin)
        {
            await KiemTraQuyenDanhGiaAsync(nguoiTaoId, laAdmin);

            if (request.NhaThau == null || request.NhaThau.Count == 0)
                throw new ApiException("Vui lòng chọn ít nhất 1 nhà thầu");

            // Gộp theo NhaThauId (giữ đoạn của lần khai báo ĐẦU TIÊN nếu trùng)
            // — cùng tinh thần Distinct() của luồng cũ.
            var nhaThauRequests = request.NhaThau.GroupBy(x => x.NhaThauId).Select(g => g.First()).ToList();
            var nhaThauIds = nhaThauRequests.Select(x => x.NhaThauId).ToList();

            var nhaThauHopLe = await _nhaThauRepository.FindAsync(x => nhaThauIds.Contains(x.Id));
            if (nhaThauHopLe.Count != nhaThauIds.Count)
                throw new ApiException("Có nhà thầu không tồn tại trong danh sách đã chọn");

            var tuNgayPhieu = request.TuNgay.Date;
            var denNgayPhieu = request.DenNgay.Date;
            if (tuNgayPhieu > denNgayPhieu)
                throw new ApiException("Từ ngày phải trước hoặc bằng Đến ngày");

            // Số hiệu reset theo năm hiện tại (không theo nhà thầu, xem SinhSoHieuAsync).
            var soHieu = await SinhSoHieuAsync(DateTime.Now.Year);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var phieu = new Phieu4TongHop
                {
                    SoHieu    = soHieu,
                    TuNgay    = tuNgayPhieu,
                    DenNgay   = denNgayPhieu,
                    NguoiTao  = nguoiTaoId,
                    TrangThai = "NHAP",
                    NgayTao   = DateTime.Now,
                };
                await _phieuRepository.AddAsync(phieu);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật

                var thuTu = 0;
                var nhaThauCotMoi = new List<Phieu4NhaThau>();
                foreach (var item in nhaThauRequests)
                {
                    var cot = new Phieu4NhaThau
                    {
                        PhieuId = phieu.Id,
                        NhaThauId = item.NhaThauId,
                        ThuTu = thuTu++,
                    };
                    await _nhaThauCotRepository.AddAsync(cot);
                    nhaThauCotMoi.Add(cot);
                }
                await _unitOfWork.SaveChangesAsync();

                foreach (var (item, cot) in nhaThauRequests.Zip(nhaThauCotMoi))
                    await TaoDoanAsync(cot.Id, item.Doan, tuNgayPhieu, denNgayPhieu);

                await KhoiTaoBang1Async(phieu.Id, nhaThauIds);

                var bang2 = new Phieu4Bang { PhieuId = phieu.Id, SoBang = 2, TenBang = "2. Tổng hợp kết quả đánh giá từ phòng chức năng" };
                await _bangRepository.AddAsync(bang2);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật để seed dòng
                await DongBoBang2CoDinhAsync(bang2, nhaThauIds);

                await transaction.CommitAsync();

                await TinhLaiAsync(phieu.Id);

                return await LayChiTietAsync(phieu.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================================
        // XÓA
        // ============================================================

        // laAdmin bypass ràng buộc trạng thái — xem Phieu1Service.XoaAsync.
        public async Task XoaAsync(int id, bool laAdmin)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);
            if (!laAdmin && phieu.TrangThai != "NHAP")
                throw new ApiException("Chỉ có thể xóa phiếu ở trạng thái Nháp");

            var bang = await _bangRepository.FindAsync(x => x.PhieuId == id);
            var bangIds = bang.Select(x => x.Id).ToList();
            var dong = await _dongRepository.FindAsync(x => bangIds.Contains(x.BangId));
            var dongIds = dong.Select(x => x.Id).ToList();
            var giaTri = await _giaTriRepository.FindAsync(x => dongIds.Contains(x.DongId));

            _giaTriRepository.RemoveRange(giaTri);
            _dongRepository.RemoveRange(dong);
            _bangRepository.RemoveRange(bang);

            var nhaThau = await _nhaThauCotRepository.FindAsync(x => x.PhieuId == id);
            var nhaThauCotIds = nhaThau.Select(x => x.Id).ToList();
            if (nhaThauCotIds.Count > 0)
            {
                var doan = await _doanRepository.FindAsync(x => nhaThauCotIds.Contains(x.NhaThauCotId));
                var doanIds = doan.Select(x => x.Id).ToList();
                if (doanIds.Count > 0)
                {
                    var doanDiaDiem = await _doanDiaDiemRepository.FindAsync(x => doanIds.Contains(x.DoanId));
                    _doanDiaDiemRepository.RemoveRange(doanDiaDiem);
                }
                _doanRepository.RemoveRange(doan);
            }
            _nhaThauCotRepository.RemoveRange(nhaThau);

            var chuKy = await _chuKyPhieuRepository.FindAsync(x => x.LoaiDoiTuong == "PHIEU4" && x.DoiTuongId == id);
            _chuKyPhieuRepository.RemoveRange(chuKy);

            _phieuRepository.Remove(phieu);
            await _unitOfWork.SaveChangesAsync();
        }

        // ============================================================
        // THÊM 1 CỘT NHÀ THẦU VÀO PHIẾU ĐÃ LẬP
        // ============================================================

        public async Task<Phieu4ResponseDto> ThemNhaThauAsync(int id, int nhaThauId)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể thêm nhà thầu khi phiếu ở trạng thái Nháp hoặc Từ chối");

            _ = await _nhaThauRepository.GetByIdAsync(nhaThauId)
                ?? throw new ApiException("Không tìm thấy nhà thầu");

            var danhSachHienTai = await _nhaThauCotRepository.FindAsync(x => x.PhieuId == id);
            if (danhSachHienTai.Any(x => x.NhaThauId == nhaThauId))
                throw new ApiException("Nhà thầu này đã có trong phiếu");

            var thuTuKeTiep = danhSachHienTai.Count == 0 ? 0 : danhSachHienTai.Max(x => x.ThuTu) + 1;
            await _nhaThauCotRepository.AddAsync(new Phieu4NhaThau { PhieuId = id, NhaThauId = nhaThauId, ThuTu = thuTuKeTiep });
            await _unitOfWork.SaveChangesAsync();

            // Tạo ô giá trị rỗng cho nhà thầu mới ở mọi dòng của mọi bảng (1-5) —
            // Bảng 1 và Bảng 2-5 luôn đi cùng bộ cột nhà thầu như nhau.
            var bang = await _bangRepository.FindAsync(x => x.PhieuId == id);
            var bangIds = bang.Select(x => x.Id).ToList();
            var dong = await _dongRepository.FindAsync(x => bangIds.Contains(x.BangId));
            foreach (var d in dong)
                await _giaTriRepository.AddAsync(new Phieu4GiaTri { DongId = d.Id, NhaThauId = nhaThauId });
            await _unitOfWork.SaveChangesAsync();

            // Tính lại ngay để Bảng 1 (nhóm 2/3/4) của cột mới có dữ liệu tự động
            return await TinhLaiAsync(id);
        }

        // ============================================================
        // XÓA 1 CỘT NHÀ THẦU KHỎI PHIẾU ĐÃ LẬP
        // ============================================================

        public async Task<Phieu4ResponseDto> XoaNhaThauAsync(int id, int nhaThauId)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể xóa nhà thầu khi phiếu ở trạng thái Nháp hoặc Từ chối");

            var danhSachHienTai = await _nhaThauCotRepository.FindAsync(x => x.PhieuId == id);
            var cot = danhSachHienTai.FirstOrDefault(x => x.NhaThauId == nhaThauId)
                ?? throw new ApiException("Nhà thầu này không có trong phiếu");
            if (danhSachHienTai.Count <= 1)
                throw new ApiException("Phiếu phải có ít nhất 1 nhà thầu — không thể xóa nhà thầu cuối cùng");

            var bang = await _bangRepository.FindAsync(x => x.PhieuId == id);
            var bangIds = bang.Select(x => x.Id).ToList();
            var dong = await _dongRepository.FindAsync(x => bangIds.Contains(x.BangId));
            var dongIds = dong.Select(x => x.Id).ToList();

            // Bảng 1-3: xóa toàn bộ ô giá trị của nhà thầu này (mọi dòng).
            var giaTriCuaNhaThau = await _giaTriRepository.FindAsync(x => dongIds.Contains(x.DongId) && x.NhaThauId == nhaThauId);
            _giaTriRepository.RemoveRange(giaTriCuaNhaThau);

            // Bảng 5 (chỉ còn ở phiếu cũ tạo trước khi bỏ Bảng 3/4/5): dòng gắn
            // CỨNG với nhà thầu này (Phieu4Dong.NhaThauId, khác nghĩa
            // Phieu4GiaTri.NhaThauId — xem comment ở Phieu4Dong.cs) — xóa luôn
            // để không còn nhóm "ma" của 1 nhà thầu đã bị bỏ khỏi phiếu.
            var dongBang5CuaNhaThau = dong.Where(x => x.NhaThauId == nhaThauId).ToList();
            if (dongBang5CuaNhaThau.Count > 0)
                _dongRepository.RemoveRange(dongBang5CuaNhaThau);

            // Đoạn thời gian/địa điểm của riêng cột này.
            var doan = await _doanRepository.FindAsync(x => x.NhaThauCotId == cot.Id);
            var doanIds = doan.Select(x => x.Id).ToList();
            if (doanIds.Count > 0)
            {
                var doanDiaDiem = await _doanDiaDiemRepository.FindAsync(x => doanIds.Contains(x.DoanId));
                _doanDiaDiemRepository.RemoveRange(doanDiaDiem);
            }
            _doanRepository.RemoveRange(doan);

            _nhaThauCotRepository.Remove(cot);

            // Dồn lại ThuTu liên tục cho các cột còn lại, giữ nguyên thứ tự
            // tương đối giữa chúng.
            var conLai = danhSachHienTai.Where(x => x.NhaThauId != nhaThauId).OrderBy(x => x.ThuTu).ToList();
            for (var i = 0; i < conLai.Count; i++)
            {
                if (conLai[i].ThuTu == i) continue;
                conLai[i].ThuTu = i;
                _nhaThauCotRepository.Update(conLai[i]);
            }

            await _unitOfWork.SaveChangesAsync();

            return await TinhLaiAsync(id);
        }

        // ============================================================
        // TÍNH LẠI BẢNG 1 TỰ ĐỘNG (từ Phieu2_DanhGia)
        // ============================================================

        public async Task<Phieu4ResponseDto> TinhLaiAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);

            var danhSachNhaThau = await _nhaThauCotRepository.FindAsync(x => x.PhieuId == id);
            var bang1 = (await _bangRepository.FindAsync(x => x.PhieuId == id && x.SoBang == 1))
                        .FirstOrDefault();
            if (bang1 == null) return await LayChiTietAsync(id);

            var dongBang1 = await _dongRepository.FindAsync(x => x.BangId == bang1.Id);
            var dongNhom1 = dongBang1.FirstOrDefault(x => x.NhomSo == 1);
            var dongNhom2 = dongBang1.Where(x => x.NhomSo == 2).OrderBy(x => x.Stt).ToList();
            var dongNhom3 = dongBang1.FirstOrDefault(x => x.NhomSo == 3);
            var dongNhom4 = dongBang1.FirstOrDefault(x => x.NhomSo == 4);

            var dongIdsBang1 = dongBang1.Select(x => x.Id).ToList();
            var giaTriBang1 = await _giaTriRepository.FindAsync(x => dongIdsBang1.Contains(x.DongId));

            foreach (var cot in danhSachNhaThau)
            {
                var doanCalc = await LayDoanCalcTheoCotAsync(cot.Id);
                var soLuotTheoMuc = await TinhSoLuotCbnvTheoMucTuDoanAsync(doanCalc);
                var tongLuot = soLuotTheoMuc[1] + soLuotTheoMuc[2] + soLuotTheoMuc[3] + soLuotTheoMuc[4] + soLuotTheoMuc[5];

                // Nhóm 2: 5 dòng, mỗi dòng ứng với 1 mức điểm (Stt = mức)
                foreach (var d in dongNhom2)
                {
                    var muc = d.Stt ?? 0;
                    if (muc is < 1 or > 5) continue;
                    await GhiGiaTriTuDongAsync(giaTriBang1, d.Id, cot.NhaThauId, soLuotTheoMuc[muc]);
                }

                // Nhóm 1: Tổng số suất ăn — tự động = tổng DuLieuCom.Com_ThucTe_
                // {Sang,Trua,Chieu,Dem} theo đúng biên bữa ăn của từng đoạn/địa
                // điểm đã khai báo cho cột này.
                if (dongNhom1 != null)
                {
                    var tongSuatAn = await TinhTongSuatAnTuDoanAsync(doanCalc);
                    await GhiGiaTriTuDongAsync(giaTriBang1, dongNhom1.Id, cot.NhaThauId, tongSuatAn);
                }

                // Lấy giá trị Nhóm 1 SAU khi ghi tự động ở trên (giữ nguyên nếu ô
                // đã bị sửa tay — xem GhiGiaTriTuDongAsync) để tính nhóm 3/4.
                var giaTriNhom1 = dongNhom1 != null
                    ? giaTriBang1.FirstOrDefault(g => g.DongId == dongNhom1.Id && g.NhaThauId == cot.NhaThauId)?.GiaTri
                    : null;

                // Nhóm 3: điểm đánh giá trung bình = SUMPRODUCT(1..5, sl) / tổng lượt
                if (dongNhom3 != null)
                {
                    decimal? diemTb = tongLuot > 0
                        ? Math.Round((1 * soLuotTheoMuc[1] + 2 * soLuotTheoMuc[2] + 3 * soLuotTheoMuc[3] + 4 * soLuotTheoMuc[4] + 5 * soLuotTheoMuc[5]) / tongLuot, 2)
                        : null;
                    await GhiGiaTriTuDongAsync(giaTriBang1, dongNhom3.Id, cot.NhaThauId, diemTb);
                }

                // Nhóm 4: tỷ lệ CBNV tham gia = tổng nhóm 2 / nhóm 1 x 100%
                if (dongNhom4 != null)
                {
                    decimal? tyLe = giaTriNhom1.HasValue && giaTriNhom1.Value > 0
                        ? Math.Round(tongLuot / giaTriNhom1.Value * 100, 2)
                        : null;
                    await GhiGiaTriTuDongAsync(giaTriBang1, dongNhom4.Id, cot.NhaThauId, tyLe);
                }
            }

            await TinhLaiBang2Async(phieu, danhSachNhaThau);

            await _unitOfWork.SaveChangesAsync();
            return await LayChiTietAsync(id);
        }

        // ============================================================
        // TÍNH LẠI BẢNG 2 TỰ ĐỘNG (xác nhận nghiệp vụ 2026-09-01, cùng công
        // thức với Phieu3Service.TinhLaiBang2Async nhưng theo TỪNG CỘT nhà
        // thầu + khoảng ngày [TuNgay, DenNgay] thay vì Tháng/Năm)
        // ============================================================
        //
        // Dòng P.ĐN (NhomSo=1, Stt 1-6): TC1,TC2,TC4,TC5,TC6 TỰ ĐỘNG = TB
        // Phieu2_TieuChi.Diem (đúng MaTieuChi) của các Phiếu 2 nhà thầu này
        // lập trong khoảng ngày.
        // Dòng P.ATMT (NhomSo=2, Stt 7-12): CHỈ Stt=7 (VSATTP) TỰ ĐỘNG = TB
        // Phieu1_KetLuan.DiemDanhGia của các Phiếu 1 do P.ATMT lập cho nhà
        // thầu này trong khoảng ngày (Phiếu 1 chỉ đo VSATTP).
        // Stt=3/9 "Đa dạng thực đơn" không có nguồn tự động — luôn nhập tay.
        private async Task TinhLaiBang2Async(Phieu4TongHop phieu, List<Phieu4NhaThau> danhSachNhaThauCot)
        {
            var bang2 = (await _bangRepository.FindAsync(x => x.PhieuId == phieu.Id && x.SoBang == 2)).FirstOrDefault();
            if (bang2 == null) return;

            var dongBang2 = await _dongRepository.FindAsync(x => x.BangId == bang2.Id);
            var dongIdsBang2 = dongBang2.Select(x => x.Id).ToList();
            var giaTriBang2 = await _giaTriRepository.FindAsync(x => dongIdsBang2.Contains(x.DongId));

            var pbAtmt = await _phongBanRepository.FirstOrDefaultAsync(x => x.Ma == "PATMT");

            foreach (var cot in danhSachNhaThauCot)
            {
                // Union ngày của TẤT CẢ đoạn của RIÊNG cột này — Bảng 2 không
                // quan tâm địa điểm/bữa ăn bắt đầu-kết thúc của đoạn, chỉ NGÀY.
                var doanRanges = await LayDoanRangesTheoCotAsync(cot.Id);

                // ---- P.ĐN (NhomSo = 1) ----
                // Chỉ tính từ Phiếu 2 đã DUYỆT — Nháp/Chờ ký chưa phải số liệu
                // chính thức, không được đưa vào trung bình Bảng 2.
                var phieu2CuaNhaThau = (await _phieu2Repository.FindAsync(x => x.NhaThauId == cot.NhaThauId && x.TrangThai == "DA_DUYET"))
                    .Where(x => x.ThoiGianTu.HasValue && TrongDoanNao(x.ThoiGianTu.Value.Date, doanRanges))
                    .ToList();
                var phieu2Ids = phieu2CuaNhaThau.Select(x => x.Id).ToHashSet();
                var tieuChiPhieu2 = phieu2Ids.Count > 0
                    ? await _phieu2TieuChiRepository.FindAsync(x => phieu2Ids.Contains(x.PhieuId))
                    : new List<Phieu2TieuChi>();

                foreach (var tc in TieuChiBang2)
                {
                    if (tc.MaTieuChiPhieu2 == null) continue; // Đa dạng thực đơn — luôn nhập tay
                    var dong = dongBang2.FirstOrDefault(x => x.NhomSo == 1 && x.Stt == tc.Stt);
                    if (dong == null) continue;

                    decimal? giaTri;
                    if (tc.MaTieuChiPhieu2 == "VSATTP")
                    {
                        // VSATTP: Phieu2_TieuChi.Diem đã có sẵn điểm số thật (lấy từ
                        // Phiếu 1 liên kết hoặc nhập tay khi Đạt) -> TB trực tiếp.
                        var cacDiem = tieuChiPhieu2
                            .Where(x => x.MaTieuChi == tc.MaTieuChiPhieu2 && x.Diem.HasValue)
                            .Select(x => x.Diem!.Value)
                            .ToList();
                        giaTri = cacDiem.Count > 0 ? Math.Round(cacDiem.Average(), 2) : null;
                    }
                    else
                    {
                        // Các tiêu chí còn lại chỉ có Đạt/Không đạt ở Phiếu 2, KHÔNG có
                        // điểm số -> quy đổi = số Phiếu 2 Đạt tiêu chí này / số Phiếu 2
                        // CÓ ĐÁNH GIÁ tiêu chí này (Dat hoặc KhongDat) × 5 — xác nhận
                        // nghiệp vụ 2026-09-14, sửa lại: Phiếu 2 bỏ trống (chưa đánh
                        // giá) tiêu chí này không còn bị tính ngầm là "không đạt" nữa,
                        // loại hẳn khỏi cả tử số lẫn mẫu số (giống Phieu3Service.TinhLaiBang2Async).
                        var soPhieuDanhGia = tieuChiPhieu2.Count(x => x.MaTieuChi == tc.MaTieuChiPhieu2 && (x.Dat || x.KhongDat));
                        var soLuongDat = tieuChiPhieu2.Count(x => x.MaTieuChi == tc.MaTieuChiPhieu2 && x.Dat);
                        giaTri = soPhieuDanhGia > 0 ? Math.Round((decimal)soLuongDat / soPhieuDanhGia * 5, 2) : null;
                    }
                    await GhiGiaTriTuDongAsync(giaTriBang2, dong.Id, cot.NhaThauId, giaTri);
                }

                // ---- P.ATMT (NhomSo = 2) — chỉ VSATTP (Stt = 7) ----
                if (pbAtmt != null)
                {
                    var dongVsattpAtmt = dongBang2.FirstOrDefault(x => x.NhomSo == 2 && x.Stt == 7);
                    if (dongVsattpAtmt != null)
                    {
                        // Chỉ tính từ Phiếu 1 đã DUYỆT — cùng quy tắc với Phiếu 2 ở trên.
                        var phieu1CuaKhoangNgay = (await _phieu1Repository.FindAsync(x =>
                                x.NhaThauId == cot.NhaThauId && x.PhongBanId == pbAtmt.Id && x.TrangThai == "DA_DUYET"))
                            .Where(x => TrongDoanNao(x.NgayKiemTra.Date, doanRanges))
                            .ToList();
                        var phieu1Ids = phieu1CuaKhoangNgay.Select(x => x.Id).ToHashSet();
                        var ketLuanCuaKhoangNgay = phieu1Ids.Count > 0
                            ? await _phieu1KetLuanRepository.FindAsync(x => phieu1Ids.Contains(x.PhieuId))
                            : new List<Phieu1KetLuan>();

                        var cacDiem = ketLuanCuaKhoangNgay
                            .Where(x => x.DiemDanhGia.HasValue)
                            .Select(x => x.DiemDanhGia!.Value)
                            .ToList();
                        decimal? giaTri = cacDiem.Count > 0 ? Math.Round(cacDiem.Average(), 2) : null;
                        await GhiGiaTriTuDongAsync(giaTriBang2, dongVsattpAtmt.Id, cot.NhaThauId, giaTri);
                    }
                }

                // NhomSo=3, Stt=13 "Điểm đánh giá trung bình của phòng ban theo
                // trọng số" KHÔNG ghi/lưu ở đây nữa — luôn tính LIVE mỗi lần đọc
                // phiếu (xem ChiTietAsync/TinhTrongSoBang2ChoDongThau) để tự
                // nhảy theo đúng số liệu thực tế, kể cả TC3 "Đa dạng thực đơn"
                // vừa được BP.QLTT sửa tay qua CapNhatGiaTriAsync — không cần
                // đợi bấm "Làm mới".
            }
        }

        // ============================================================
        // ĐOẠN THỜI GIAN — helper dùng chung cho Bảng 1 (thay thế hoàn toàn
        // suy luận "địa điểm rõ ràng" cũ)
        // ============================================================

        // Model tính toán nội bộ (không lưu DB) dựng từ Phieu4Doan +
        // Phieu4DoanDiaDiem — TuThuTu/DenThuTu là thứ tự bữa ăn 1..4 (Sáng..Đêm).
        private record DoanCalc(DateTime TuNgay, int TuThuTu, DateTime DenNgay, int DenThuTu, HashSet<int> DiaDiemIds);

        // Bỏ dòng "ALL" (tổng cộng, dùng cho DuLieuCom.Com_ThucTe_ALL — không
        // phải bữa ăn thật, CodeBuaAn không parse ra số được) — chỉ giữ 4 bữa
        // Sáng/Trưa/Chiều/Đêm (CodeBuaAn "01".."04").
        private async Task<Dictionary<int, int>> LayBuaAnThuTuAsync()
        {
            var buaAn = await _buaAnRepository.GetAllAsync();
            return buaAn
                .Where(b => int.TryParse(b.CodeBuaAn, out _))
                .ToDictionary(b => b.ID, b => int.Parse(b.CodeBuaAn));
        }

        private async Task<List<DoanCalc>> LayDoanCalcTheoCotAsync(int nhaThauCotId)
        {
            var doanEntities = await _doanRepository.FindAsync(x => x.NhaThauCotId == nhaThauCotId);
            if (doanEntities.Count == 0) return new List<DoanCalc>();

            var doanIds = doanEntities.Select(x => x.Id).ToList();
            var diaDiem = await _doanDiaDiemRepository.FindAsync(x => doanIds.Contains(x.DoanId));
            var buaAnThuTu = await LayBuaAnThuTuAsync();

            return doanEntities.Select(d => new DoanCalc(
                d.TuNgay.Date,
                buaAnThuTu.TryGetValue(d.TuBuaAnId, out var tu) ? tu : 1,
                d.DenNgay.Date,
                buaAnThuTu.TryGetValue(d.DenBuaAnId, out var den) ? den : 4,
                diaDiem.Where(x => x.DoanId == d.Id).Select(x => x.DiaDiemNhaAnId).ToHashSet()
            )).ToList();
        }

        private async Task<List<(DateTime TuNgay, DateTime DenNgay)>> LayDoanRangesTheoCotAsync(int nhaThauCotId)
        {
            var doan = await _doanRepository.FindAsync(x => x.NhaThauCotId == nhaThauCotId);
            return doan.Select(d => (d.TuNgay.Date, d.DenNgay.Date)).ToList();
        }

        // Bảng 2 chỉ quan tâm NGÀY (không quan tâm bữa ăn bắt đầu/kết thúc của
        // đoạn) — xác nhận nghiệp vụ.
        private static bool TrongDoanNao(DateTime ngay, List<(DateTime TuNgay, DateTime DenNgay)> doanRanges) =>
            doanRanges.Any(r => ngay >= r.TuNgay && ngay <= r.DenNgay);

        // Biên bữa ăn (thứ tự 1..4) được tính cho 1 ngày cụ thể trong 1 đoạn —
        // CẢ 2 đầu đoạn đều BAO GỒM bữa được chọn (xác nhận nghiệp vụ); ngày
        // giữa đoạn tính đủ 4 bữa.
        private static (int Min, int Max) BienBuaAnTrongNgay(DateTime ngay, DoanCalc doan)
        {
            var laNgayDau = ngay == doan.TuNgay;
            var laNgayCuoi = ngay == doan.DenNgay;
            if (laNgayDau && laNgayCuoi) return (doan.TuThuTu, doan.DenThuTu);
            if (laNgayDau) return (doan.TuThuTu, 4);
            if (laNgayCuoi) return (1, doan.DenThuTu);
            return (1, 4);
        }

        // tuNgayPhieu/denNgayPhieu: khoảng ngày lập phiếu CỐ ĐỊNH (chọn lúc
        // tạo, xem ThemAsync) — mọi đoạn (kể cả thêm sau ở trang chi tiết,
        // xem ThemDoanAsync) đều phải nằm TRỌN trong khoảng này, khớp đúng
        // ràng buộc DatePicker ở FE (DoanBuilder.tsx ngayToiThieu/ngayToiDa).
        private static void KiemTraDoanHopLe(DoanRequest req, Dictionary<int, int> buaAnThuTu, DateTime tuNgayPhieu, DateTime denNgayPhieu)
        {
            if (!buaAnThuTu.ContainsKey(req.TuBuaAnId))
                throw new ApiException("Bữa ăn bắt đầu không hợp lệ");
            if (!buaAnThuTu.ContainsKey(req.DenBuaAnId))
                throw new ApiException("Bữa ăn kết thúc không hợp lệ");

            var tuNgay = req.TuNgay.Date;
            var denNgay = req.DenNgay.Date;
            var hopLe = tuNgay < denNgay || (tuNgay == denNgay && buaAnThuTu[req.TuBuaAnId] <= buaAnThuTu[req.DenBuaAnId]);
            if (!hopLe)
                throw new ApiException("Đoạn kết thúc phải sau đoạn bắt đầu");

            if (tuNgay < tuNgayPhieu || denNgay > denNgayPhieu)
                throw new ApiException(
                    $"Đoạn phải nằm trong khoảng ngày lập phiếu ({tuNgayPhieu:dd/MM/yyyy} - {denNgayPhieu:dd/MM/yyyy})");
        }

        // Tạo các đoạn khai báo cho 1 cột nhà thầu — dùng trong ThemAsync, bên
        // trong transaction (mỗi đoạn cần Id thật trước khi thêm địa điểm con).
        private async Task TaoDoanAsync(int nhaThauCotId, List<DoanRequest> danhSachDoan, DateTime tuNgayPhieu, DateTime denNgayPhieu)
        {
            if (danhSachDoan.Count == 0) return;
            var buaAnThuTu = await LayBuaAnThuTuAsync();

            foreach (var req in danhSachDoan)
            {
                KiemTraDoanHopLe(req, buaAnThuTu, tuNgayPhieu, denNgayPhieu);
                var doan = new Phieu4Doan
                {
                    NhaThauCotId = nhaThauCotId,
                    TuNgay = req.TuNgay.Date,
                    TuBuaAnId = req.TuBuaAnId,
                    DenNgay = req.DenNgay.Date,
                    DenBuaAnId = req.DenBuaAnId,
                };
                await _doanRepository.AddAsync(doan);
                await _unitOfWork.SaveChangesAsync();
                foreach (var diaDiemId in req.DiaDiemNhaAnIds.Distinct())
                    await _doanDiaDiemRepository.AddAsync(new Phieu4DoanDiaDiem { DoanId = doan.Id, DiaDiemNhaAnId = diaDiemId });
            }
        }

        // ============================================================
        // THÊM / XÓA ĐOẠN THỜI GIAN (chỉ khi NHAP/TU_CHOI) — theo TỪNG CỘT
        // nhà thầu, thao tác xong tính lại Bảng 1 ngay.
        // ============================================================

        public async Task<Phieu4ResponseDto> ThemDoanAsync(int id, int nhaThauId, DoanRequest request)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể sửa đoạn khi phiếu ở trạng thái Nháp hoặc Từ chối");

            var cot = await _nhaThauCotRepository.FirstOrDefaultAsync(x => x.PhieuId == id && x.NhaThauId == nhaThauId)
                ?? throw new ApiException("Nhà thầu này không có trong phiếu");

            var buaAnThuTu = await LayBuaAnThuTuAsync();
            KiemTraDoanHopLe(request, buaAnThuTu, phieu.TuNgay, phieu.DenNgay);

            var doan = new Phieu4Doan
            {
                NhaThauCotId = cot.Id,
                TuNgay = request.TuNgay.Date,
                TuBuaAnId = request.TuBuaAnId,
                DenNgay = request.DenNgay.Date,
                DenBuaAnId = request.DenBuaAnId,
            };
            await _doanRepository.AddAsync(doan);
            await _unitOfWork.SaveChangesAsync();
            foreach (var diaDiemId in request.DiaDiemNhaAnIds.Distinct())
                await _doanDiaDiemRepository.AddAsync(new Phieu4DoanDiaDiem { DoanId = doan.Id, DiaDiemNhaAnId = diaDiemId });
            await _unitOfWork.SaveChangesAsync();

            return await TinhLaiAsync(id);
        }

        public async Task<Phieu4ResponseDto> XoaDoanAsync(int id, int nhaThauId, int doanId)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể sửa đoạn khi phiếu ở trạng thái Nháp hoặc Từ chối");

            var cot = await _nhaThauCotRepository.FirstOrDefaultAsync(x => x.PhieuId == id && x.NhaThauId == nhaThauId)
                ?? throw new ApiException("Nhà thầu này không có trong phiếu");

            var doan = await _doanRepository.GetByIdAsync(doanId);
            if (doan == null || doan.NhaThauCotId != cot.Id)
                throw new ApiException("Không tìm thấy đoạn", StatusCodes.Status404NotFound);

            var diaDiem = await _doanDiaDiemRepository.FindAsync(x => x.DoanId == doanId);
            _doanDiaDiemRepository.RemoveRange(diaDiem);
            _doanRepository.Remove(doan);
            await _unitOfWork.SaveChangesAsync();

            return await TinhLaiAsync(id);
        }

        // ============================================================
        // TÍNH SỐ LƯỢT ĐÁNH GIÁ CBNV (MỨC 1-5) TỪ KETQUADANHGIA (HỆ KIOSK CŨ)
        // ============================================================
        //
        // ID_BuaAn đã được gán đáng tin cậy ngay lúc kiosk ghi bản ghi (xem
        // EvaluatesController.Post — tra KhungGioDanhGia theo giờ hiện tại;
        // nếu giờ nộp nằm ngoài cả 4 khung giờ thì bản ghi KHÔNG được lưu vào
        // DB) — nên đọc thẳng ID_BuaAn, KHÔNG cần re-derive qua KhungGioDanhGia
        // ở đây. Cộng theo đúng biên bữa ăn của từng đoạn/địa điểm đã khai báo
        // cho cột nhà thầu này (thay thế hoàn toàn suy luận "địa điểm rõ ràng" cũ).
        private async Task<decimal[]> TinhSoLuotCbnvTheoMucTuDoanAsync(List<DoanCalc> doanList)
        {
            var soLuot = new decimal[6]; // [0] không dùng, [1..5]
            if (doanList.Count == 0) return soLuot;
            var diaDiemIds = doanList.SelectMany(d => d.DiaDiemIds).Distinct().ToList();
            if (diaDiemIds.Count == 0) return soLuot;

            var tuNgayNhoNhat = doanList.Min(d => d.TuNgay);
            var denNgayLonNhat = doanList.Max(d => d.DenNgay);
            var ketQua = await _ketQuaDanhGiaRepository.FindAsync(x =>
                diaDiemIds.Contains(x.DiaDiem_ID) &&
                x.ThoiGianDanhGia.Date >= tuNgayNhoNhat && x.ThoiGianDanhGia.Date <= denNgayLonNhat);
            var buaAnThuTu = await LayBuaAnThuTuAsync();

            foreach (var doan in doanList)
            {
                if (doan.DiaDiemIds.Count == 0) continue;
                var rowsCuaDoan = ketQua.Where(r =>
                    doan.DiaDiemIds.Contains(r.DiaDiem_ID) && r.ThoiGianDanhGia.Date >= doan.TuNgay && r.ThoiGianDanhGia.Date <= doan.DenNgay);
                foreach (var row in rowsCuaDoan)
                {
                    if (row.DiemDanhGia is < 1 or > 5) continue;
                    if (!row.ID_BuaAn.HasValue || !buaAnThuTu.TryGetValue(row.ID_BuaAn.Value, out var thuTu)) continue; // phòng thủ, không nên xảy ra
                    var (min, max) = BienBuaAnTrongNgay(row.ThoiGianDanhGia.Date, doan);
                    if (thuTu >= min && thuTu <= max) soLuot[row.DiemDanhGia]++;
                }
            }
            return soLuot;
        }

        // ============================================================
        // TÍNH TỔNG SUẤT ĂN TỪ DULIEUCOM (HỆ ĐĂNG KÝ CƠM CŨ)
        // ============================================================
        //
        // Mapping CodeBuaAn -> field DuLieuCom PHẢI theo TÊN (không theo thứ
        // tự khai báo field trong class — field là ALL,Sang,Trua,Dem,Chieu,
        // Đêm khai TRƯỚC Chiều, khác thứ tự thời gian thật 03=Chiều,04=Đêm) —
        // khớp đúng cách RiceDataController gán dữ liệu gốc từ hệ ngoài.
        private static int? ComTheoMa(DuLieuCom d, int thuTu) => thuTu switch
        {
            1 => d.Com_ThucTe_Sang,
            2 => d.Com_ThucTe_Trua,
            3 => d.Com_ThucTe_Chieu,
            4 => d.Com_ThucTe_Dem,
            _ => null,
        };

        private async Task<int> TinhTongSuatAnTuDoanAsync(List<DoanCalc> doanList)
        {
            if (doanList.Count == 0) return 0;
            var diaDiemIds = doanList.SelectMany(d => d.DiaDiemIds).Distinct().ToList();
            if (diaDiemIds.Count == 0) return 0;

            var tuNgayNhoNhat = doanList.Min(d => d.TuNgay);
            var denNgayLonNhat = doanList.Max(d => d.DenNgay);
            var duLieuCom = await _duLieuComRepository.FindAsync(x =>
                diaDiemIds.Contains(x.ID_DiemAn) && x.Ngay.Date >= tuNgayNhoNhat && x.Ngay.Date <= denNgayLonNhat);

            var tong = 0;
            foreach (var doan in doanList)
            {
                if (doan.DiaDiemIds.Count == 0) continue;
                var rowsCuaDoan = duLieuCom.Where(r =>
                    doan.DiaDiemIds.Contains(r.ID_DiemAn) && r.Ngay.Date >= doan.TuNgay && r.Ngay.Date <= doan.DenNgay);
                foreach (var row in rowsCuaDoan)
                {
                    var (min, max) = BienBuaAnTrongNgay(row.Ngay.Date, doan);
                    for (var thuTu = min; thuTu <= max; thuTu++)
                        tong += ComTheoMa(row, thuTu) ?? 0;
                }
            }
            return tong;
        }

        // ============================================================
        // SỬA TAY 1 HOẶC NHIỀU Ô
        // ============================================================

        public async Task<Phieu4ResponseDto> CapNhatGiaTriAsync(int id, Phieu4CapNhatGiaTriRequest request, int? nguoiSuaId)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể sửa phiếu ở trạng thái Nháp hoặc Từ chối");

            var bangIds = (await _bangRepository.FindAsync(x => x.PhieuId == id)).Select(x => x.Id).ToHashSet();
            var dongHopLe = (await _dongRepository.FindAsync(x => bangIds.Contains(x.BangId))).ToDictionary(x => x.Id);

            foreach (var item in request.GiaTri)
            {
                if (!dongHopLe.ContainsKey(item.DongId))
                    continue; // dòng không thuộc phiếu này — bỏ qua, không cho ghi chéo

                var oGiaTri = await _giaTriRepository.FirstOrDefaultAsync(x => x.DongId == item.DongId && x.NhaThauId == item.NhaThauId);
                if (oGiaTri == null)
                {
                    oGiaTri = new Phieu4GiaTri { DongId = item.DongId, NhaThauId = item.NhaThauId };
                    await _giaTriRepository.AddAsync(oGiaTri);
                    await _unitOfWork.SaveChangesAsync();
                }

                if (oGiaTri.GiaTri == item.GiaTri) continue;

                await _nhatKyChinhSuaService.GhiAsync(
                    "PHIEU4_GIA_TRI", oGiaTri.Id, $"NhaThau#{item.NhaThauId}",
                    oGiaTri.GiaTri?.ToString(), item.GiaTri?.ToString(), nguoiSuaId);

                oGiaTri.GiaTri = item.GiaTri;
                oGiaTri.ChinhSuaThuCong = true;
                _giaTriRepository.Update(oGiaTri);
            }

            // Bảng 4/5 — giá trị chung, không chia theo cột nhà thầu (xem
            // Phieu4Dong.GiaTriChung).
            foreach (var item in request.GiaTriChung)
            {
                if (!dongHopLe.TryGetValue(item.DongId, out var dong))
                    continue; // dòng không thuộc phiếu này — bỏ qua, không cho ghi chéo

                if (dong.GiaTriChung == item.GiaTriChung) continue;

                await _nhatKyChinhSuaService.GhiAsync(
                    "PHIEU4_GIA_TRI_CHUNG", dong.Id, null,
                    dong.GiaTriChung?.ToString(), item.GiaTriChung?.ToString(), nguoiSuaId);

                dong.GiaTriChung = item.GiaTriChung;
                dong.ChinhSuaThuCong = true;
                _dongRepository.Update(dong);
            }

            await _unitOfWork.SaveChangesAsync();
            return await LayChiTietAsync(id);
        }

        // ============================================================
        // SỬA TÊN BẢNG
        // ============================================================
        // Từ 2026-09-02, TenBang của TẤT CẢ 5 bảng đều cố định lúc tạo (xem
        // ThemAsync) giống Bảng 1 — FE không còn hiển thị chức năng "Sửa tên
        // bảng" nữa (xem Phieu4FormPage.tsx). Endpoint vẫn giữ lại (không
        // dùng tới) để không phải đổi route/quyền, phòng trường hợp cần sửa
        // tay 1 lần qua Postman/DB tool.

        public async Task<Phieu4ResponseDto> SuaBangAsync(int id, int bangId, Phieu4BangRequest request)
        {
            var bang = await LayBangHopLeAsync(id, bangId, choPhepBang1: true);
            bang.TenBang = request.TenBang;
            _bangRepository.Update(bang);
            await _unitOfWork.SaveChangesAsync();

            return await LayChiTietAsync(id);
        }

        // ============================================================
        // GỬI KÝ / ĐỒNG BỘ TRẠNG THÁI
        // ============================================================

        public async Task<Phieu4TongHop> GuiKyAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Phiếu không ở trạng thái phù hợp để gửi ký");

            await _chuKyPhieuService.KhoiTaoLuongKyAsync("PHIEU4", id);
            phieu.TrangThai = "CHO_KY";
            _phieuRepository.Update(phieu);
            await _unitOfWork.SaveChangesAsync();
            return phieu;
        }

        public async Task<Phieu4TongHop> DongBoTrangThaiAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);

            var trangThaiMoi = await _chuKyPhieuService.TrangThaiTongAsync("PHIEU4", id);
            if (trangThaiMoi is "DA_DUYET" or "TU_CHOI" or "CHO_KY")
            {
                phieu.TrangThai = trangThaiMoi;
                _phieuRepository.Update(phieu);
                await _unitOfWork.SaveChangesAsync();
            }
            return phieu;
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private async Task<string> SinhSoHieuAsync(int nam)
        {
            // Phiếu 4 reset số hiệu theo NĂM, không theo nhà thầu (Thang = null,
            // PhamVi = null) — xem mục 5.3 PhanTichNghiepVu.md.
            var seq = await _soHieuService.SinhSoTiepTheoAsync("PHIEU4", null, nam, null);
            return $"TH-{nam}-{seq:D2}";
        }

        private async Task KhoiTaoBang1Async(int phieuId, List<int> nhaThauIds)
        {
            var bang1 = new Phieu4Bang { PhieuId = phieuId, SoBang = 1, TenBang = "1. Tổng hợp kết quả đánh giá từ CBNV" };
            await _bangRepository.AddAsync(bang1);
            await _unitOfWork.SaveChangesAsync();

            var dongMoi = new List<Phieu4Dong>
            {
                new() { BangId = bang1.Id, NhomSo = 1, Stt = 1, NoiDung = "Tổng số suất ăn", Dvt = "Suất", LoaiDong = "TINH_TU_DULIEUCOM" },
            };
            for (var muc = 1; muc <= 5; muc++)
            {
                dongMoi.Add(new Phieu4Dong
                {
                    BangId = bang1.Id, NhomSo = 2, Stt = muc,
                    NoiDung = $"Số lượt đánh giá mức {muc}", Dvt = "Lượt", LoaiDong = "DEM_TU_PHIEU2",
                });
            }
            dongMoi.Add(new Phieu4Dong
            {
                BangId = bang1.Id, NhomSo = 3, Stt = 1, NoiDung = "Điểm đánh giá trung bình", Dvt = "Điểm",
                LoaiDong = "TINH_TRUNG_BINH", CongThuc = "(1*sl1+2*sl2+3*sl3+4*sl4+5*sl5)/tổng lượt (nhóm 2)",
            });
            dongMoi.Add(new Phieu4Dong
            {
                BangId = bang1.Id, NhomSo = 4, Stt = 1, NoiDung = "Tỷ lệ CBNV tham gia đánh giá", Dvt = "%",
                LoaiDong = "TINH_TY_LE", CongThuc = "Tổng nhóm 2 / Nhóm 1 × 100%",
            });

            await _dongRepository.AddRangeAsync(dongMoi);
            await _unitOfWork.SaveChangesAsync();

            var giaTriMoi = new List<Phieu4GiaTri>();
            foreach (var dong in dongMoi)
            {
                foreach (var nhaThauId in nhaThauIds)
                {
                    giaTriMoi.Add(new Phieu4GiaTri { DongId = dong.Id, NhaThauId = nhaThauId });
                }
            }
            await _giaTriRepository.AddRangeAsync(giaTriMoi);
            await _unitOfWork.SaveChangesAsync();
        }

        // Bảng 2 — CỐ ĐỊNH (không còn qua master NhomTieuChi/TieuChi, xem
        // TieuChiBang2 + TinhLaiBang2Async): NhomSo=1 (P.ĐN, Stt 1-6, đủ 6
        // tiêu chí) + NhomSo=2 (P.ATMT, CHỈ Stt=7 — đúng 1 tiêu chí VSATTP,
        // xác nhận nghiệp vụ 2026-09-01: P.ATMT chỉ đo được VSATTP qua Phiếu
        // 1, không có căn cứ cho 5 tiêu chí còn lại nên không tạo dòng cho
        // chúng nữa, tránh dòng "chết" không hiện được ở đâu cả). Stt liên
        // tục toàn bảng (không lặp lại giữa 2 nhóm) để FE (sort theo Stt khi
        // dòng rơi vào "dongKhongNhom") không bị xáo trộn.
        // Đồng bộ idempotent: chỉ thêm dòng (NhomSo,Stt) CHƯA có, an toàn gọi
        // lại nhiều lần (lúc tạo phiếu mới lẫn mỗi lần ChiTietAsync). Giúp
        // phiếu tạo TRƯỚC đợt đổi Bảng 2 sang cố định (2026-09-01, khi Bảng 2
        // còn theo master TieuChi cũ) tự có đủ dòng khi mở lại, không cần
        // thao tác thủ công.
        // NoiDung KHÔNG còn tiền tố "P.ĐN -"/"P.ATMT -" — FE render Bảng 2
        // thành khối riêng với header nhóm la mã I/II đúng tên phòng ban
        // (giống Bảng 1), xem Phieu4FormPage.tsx (NHAN_NHOM_BANG2).
        private async Task DongBoBang2CoDinhAsync(Phieu4Bang bang2, List<int> nhaThauIds)
        {
            var dongHienTai = await _dongRepository.FindAsync(x => x.BangId == bang2.Id);

            var dongMoi = new List<Phieu4Dong>();
            foreach (var tc in TieuChiBang2)
            {
                if (!dongHienTai.Any(x => x.NhomSo == 1 && x.Stt == tc.Stt))
                    dongMoi.Add(new Phieu4Dong { BangId = bang2.Id, NhomSo = 1, Stt = tc.Stt, NoiDung = tc.Ten, Dvt = "Điểm (1-5)", LoaiDong = "TINH_TRUNG_BINH" });
            }

            var vsattp = TieuChiBang2[0];
            if (!dongHienTai.Any(x => x.NhomSo == 2 && x.Stt == vsattp.Stt + 6))
                dongMoi.Add(new Phieu4Dong { BangId = bang2.Id, NhomSo = 2, Stt = vsattp.Stt + 6, NoiDung = vsattp.Ten, Dvt = "Điểm (1-5)", LoaiDong = "TINH_TRUNG_BINH" });

            // NhomSo=3, Stt=13: "Điểm đánh giá trung bình của phòng ban theo
            // trọng số" — thêm 2026-09-01, CHƯA xác nhận trọng số cụ thể giữa
            // P.ĐN/P.ATMT nên tạm dùng trung bình cộng KHÔNG trọng số của 7
            // dòng phía trên (Stt 1-6 nhóm P.ĐN + Stt 7 VSATTP P.ATMT). BE
            // KHÔNG tính dòng này — 6/7 dòng nguồn chỉ đổi qua "Làm mới" (auto)
            // hoặc qua sửa tay TC3 "Đa dạng thực đơn" (dòng NHẬP TAY duy nhất
            // của Bảng 2), cả 2 đường đều trả dữ liệu mới về FE ngay lập tức,
            // nên FE (Phieu4FormPage.tsx: tinhDiemTrongSo) tự tính lại mỗi lần
            // render từ dữ liệu đang có (kể cả ô TC3 đang sửa dở, chưa lưu) —
            // khi bấm "Lưu thay đổi" FE gửi kèm luôn số đã tính cho dòng này
            // trong CÙNG request CapNhatGiaTriAsync, không cần BE tính lại.
            // `Phieu4_GiaTri` của dòng này vẫn seed rỗng như dưới, chỉ để có
            // hàng ghi lại giá trị FE gửi lên — không phải nguồn hiển thị.
            if (!dongHienTai.Any(x => x.NhomSo == 3 && x.Stt == 13))
                dongMoi.Add(new Phieu4Dong { BangId = bang2.Id, NhomSo = 3, Stt = 13, NoiDung = "Điểm đánh giá trung bình của phòng ban theo trọng số", Dvt = "", LoaiDong = "TINH_TRUNG_BINH" });

            if (dongMoi.Count > 0)
            {
                await _dongRepository.AddRangeAsync(dongMoi);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật cho giá trị con
            }

            // Bù ô giá trị còn thiếu cho từng cột nhà thầu hiện tại — cả dòng cũ
            // (VD nhà thầu mới thêm sau) lẫn dòng vừa mới đồng bộ ở trên.
            var tatCaDong = dongMoi.Count > 0 ? dongHienTai.Concat(dongMoi).ToList() : dongHienTai;
            var dongIds = tatCaDong.Select(x => x.Id).ToList();
            var giaTriHienTai = dongIds.Count > 0
                ? await _giaTriRepository.FindAsync(x => dongIds.Contains(x.DongId))
                : new List<Phieu4GiaTri>();

            var giaTriMoi = new List<Phieu4GiaTri>();
            foreach (var dong in tatCaDong)
                foreach (var nhaThauId in nhaThauIds)
                    if (!giaTriHienTai.Any(g => g.DongId == dong.Id && g.NhaThauId == nhaThauId))
                        giaTriMoi.Add(new Phieu4GiaTri { DongId = dong.Id, NhaThauId = nhaThauId });

            if (giaTriMoi.Count > 0)
            {
                await _giaTriRepository.AddRangeAsync(giaTriMoi);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // Ghi giá trị tính tự động vào danh sách đã tải sẵn (tránh N+1 query) —
        // CHỈ ghi nếu ô chưa từng bị sửa tay; nếu ô chưa tồn tại thì tạo mới.
        private async Task GhiGiaTriTuDongAsync(List<Phieu4GiaTri> daTai, int dongId, int nhaThauId, decimal? giaTri)
        {
            var o = daTai.FirstOrDefault(x => x.DongId == dongId && x.NhaThauId == nhaThauId);
            if (o == null)
            {
                o = new Phieu4GiaTri { DongId = dongId, NhaThauId = nhaThauId, GiaTri = giaTri, ChinhSuaThuCong = false };
                await _giaTriRepository.AddAsync(o);
                daTai.Add(o);
                return;
            }
            if (o.ChinhSuaThuCong) return;
            o.GiaTri = giaTri;
            _giaTriRepository.Update(o);
        }

        private async Task<Phieu4Bang> LayBangHopLeAsync(int phieuId, int bangId, bool choPhepBang1)
        {
            var bang = await _bangRepository.GetByIdAsync(bangId);
            if (bang == null || bang.PhieuId != phieuId)
                throw new ApiException("Không tìm thấy bảng", StatusCodes.Status404NotFound);
            if (!choPhepBang1 && bang.SoBang == 1)
                throw new ApiException("Bảng 1 có cấu trúc cố định, không cho sửa qua endpoint này");
            return bang;
        }
    }
}
