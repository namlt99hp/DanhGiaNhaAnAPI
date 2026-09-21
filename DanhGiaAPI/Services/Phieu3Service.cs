using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Common;
using DanhGiaAPI.DTOs.Phieu3;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    // Xem quyết định thiết kế + các giả định nghiệp vụ đã xác nhận tại
    // 02. Phantich/modules/Phieu3_BaoCaoThang.md.
    //
    // Bảng 1 (3 dòng cố định):
    //   - LUOT_CBNV_THAM_GIA: TỰ ĐỘNG (đổi nguồn 2026-08-28, xem
    //     "TÍNH SỐ LƯỢT ĐÁNH GIÁ CBNV" bên dưới) — đếm KetQuaDanhGia (hệ kiosk
    //     CBNV tự chấm 1-5) tại các Nhà ăn (DiaDiemNhaAn) mà nhà thầu này phụ
    //     trách trong tháng, suy ra qua Phieu2_NhaAn (1 Phiếu 2 có thể gộp
    //     nhiều nhà ăn). Trước đây dùng
    //     proxy Phieu2_KetQua.SoTieuChiDat (đếm số tiêu chí "Đạt" trong
    //     checklist người đánh giá — không phải dữ liệu CBNV tự chấm thật).
    //   - TONG_SUAT_AN: TỰ ĐỘNG (đổi nguồn từ nhập tay, xem "TÍNH TỔNG SUẤT ĂN
    //     TỪ DULIEUCOM" bên dưới) — tổng DuLieuCom.Com_ThucTe_ALL trong tháng,
    //     tại các Nhà ăn (DiaDiemNhaAn) mà nhà thầu này phụ trách, suy ra qua
    //     Phieu2_NhaAn (cùng tập "nhà ăn rõ ràng" dùng cho LUOT_CBNV_THAM_GIA).
    //   - TY_LE_PHAN_TRAM: TỰ ĐỘNG (suy ra) = LUOT_CBNV_THAM_GIA / TONG_SUAT_AN,
    //     chỉ tính được khi TONG_SUAT_AN.Tong đã được nhập tay và > 0.
    //
    // Bảng 2 (2 dòng P.ĐN/P.ATMT x 6 tiêu chí TC1..TC6) — xác nhận nghiệp vụ
    // 2026-09-01 (xem TinhLaiBang2Async):
    //   - Dòng P.ĐN: TC1,TC2,TC4,TC5,TC6 TỰ ĐỘNG = TB Phieu2_TieuChi.Diem
    //     (đúng MaTieuChi tương ứng) của các Phiếu 2 nhà thầu trong tháng.
    //   - Dòng P.ATMT: CHỈ TC1 (VSATTP) TỰ ĐỘNG = TB Phieu1_KetLuan.DiemDanhGia
    //     của các Phiếu 1 do P.ATMT lập cho nhà thầu trong tháng (Phiếu 1 chỉ
    //     đo VSATTP, không có nguồn cho các TC khác của dòng này).
    //   - TC3 "Đa dạng thực đơn" — KHÔNG có nguồn tự động ở cả 2 dòng, luôn
    //     nhập tay.
    //
    // Quy tắc "tính lại": chỉ ghi đè các ô (Bảng 1 lẫn Bảng 2) có
    // ChinhSuaThuCong = false (chưa từng bị sửa tay) — giữ nguyên giá trị
    // người dùng đã tự nhập/sửa.
    public class Phieu3Service : IPhieu3Service
    {
        private static readonly List<(string Ma, string Ten)> Bang1DongCoDinh = new()
        {
            ("LUOT_CBNV_THAM_GIA", "Lượt CBNV tham gia đánh giá"),
            ("TONG_SUAT_AN",       "Tổng suất ăn tại chỗ"),
            ("TY_LE_PHAN_TRAM",    "Tỷ lệ % tiêu chí đạt"),
        };

        private static readonly string[] MaPhongBanBang2 = { "PDN", "PATMT" };
        private static readonly string[] MaTieuChiBang2 = { "TC1", "TC2", "TC3", "TC4", "TC5", "TC6" };

        private readonly IPhieu3BaoCaoRepository       _phieuRepository;
        private readonly IPhieu3Bang1DongRepository     _bang1Repository;
        private readonly IPhieu3Bang2DongRepository     _bang2DongRepository;
        private readonly IPhieu3Bang2GiaTriRepository   _bang2GiaTriRepository;
        private readonly IPhieu3YKienNhaThauRepository  _yKienRepository;
        private readonly IPhieu3DoanRepository          _doanRepository;
        private readonly IPhieu3DoanDiaDiemRepository   _doanDiaDiemRepository;
        private readonly IBuaAnRepository               _buaAnRepository;
        private readonly IPhieu2DanhGiaRepository       _phieu2Repository;
        private readonly IPhieu2TieuChiRepository       _phieu2TieuChiRepository;
        private readonly IPhieu1KiemTraRepository       _phieu1Repository;
        private readonly IPhieu1KetLuanRepository       _phieu1KetLuanRepository;
        private readonly IKetQuaDanhGiaRepository       _ketQuaDanhGiaRepository;
        private readonly IDuLieuComRepository           _duLieuComRepository;
        private readonly INhaThauRepository             _nhaThauRepository;
        private readonly IPhongBanRepository            _phongBanRepository;
        private readonly INguoiDungPhieuQuyenRepository _nguoiDungPhieuQuyenRepository;
        private readonly IQuyenXemPhieuService          _quyenXemPhieuService;
        private readonly ISoHieuService                 _soHieuService;
        private readonly IChuKyPhieuService              _chuKyPhieuService;
        private readonly IChuKyPhieuRepository            _chuKyPhieuRepository;
        private readonly INhatKyChinhSuaService          _nhatKyChinhSuaService;
        private readonly IUnitOfWork                     _unitOfWork;

        // Ánh xạ TC (Bảng 2) -> MaTieuChi tương ứng bên Phiếu 2 (dòng P.ĐN) —
        // xác nhận nghiệp vụ 2026-09-01. TC3 "Đa dạng thực đơn" KHÔNG có
        // tương ứng bên Phiếu 2 -> luôn nhập tay, không nằm trong bảng này.
        private static readonly Dictionary<string, string> AnhXaTcSangPhieu2 = new()
        {
            ["TC1"] = "VSATTP",
            ["TC2"] = "DINH_LUONG_THUC_DON",
            ["TC4"] = "DIEU_KHOAN_KHAC",
            ["TC5"] = "THAI_DO_PHOI_HOP",
            ["TC6"] = "PHAN_HOI_SU_CO",
        };

        public Phieu3Service(
            IPhieu3BaoCaoRepository       phieuRepository,
            IPhieu3Bang1DongRepository     bang1Repository,
            IPhieu3Bang2DongRepository     bang2DongRepository,
            IPhieu3Bang2GiaTriRepository   bang2GiaTriRepository,
            IPhieu3YKienNhaThauRepository  yKienRepository,
            IPhieu3DoanRepository          doanRepository,
            IPhieu3DoanDiaDiemRepository   doanDiaDiemRepository,
            IBuaAnRepository               buaAnRepository,
            IPhieu2DanhGiaRepository       phieu2Repository,
            IPhieu2TieuChiRepository       phieu2TieuChiRepository,
            IPhieu1KiemTraRepository       phieu1Repository,
            IPhieu1KetLuanRepository       phieu1KetLuanRepository,
            IKetQuaDanhGiaRepository       ketQuaDanhGiaRepository,
            IDuLieuComRepository           duLieuComRepository,
            INhaThauRepository             nhaThauRepository,
            IPhongBanRepository            phongBanRepository,
            INguoiDungPhieuQuyenRepository nguoiDungPhieuQuyenRepository,
            IQuyenXemPhieuService          quyenXemPhieuService,
            ISoHieuService                 soHieuService,
            IChuKyPhieuService              chuKyPhieuService,
            IChuKyPhieuRepository            chuKyPhieuRepository,
            INhatKyChinhSuaService          nhatKyChinhSuaService,
            IUnitOfWork                     unitOfWork)
        {
            _phieuRepository        = phieuRepository;
            _bang1Repository        = bang1Repository;
            _bang2DongRepository    = bang2DongRepository;
            _bang2GiaTriRepository  = bang2GiaTriRepository;
            _yKienRepository        = yKienRepository;
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
        // (trước đây Phiếu 3 KHÔNG có gate nào, mọi tài khoản nội bộ đều tạo
        // được — xem VaiTro.md mục 10).
        private async Task KiemTraQuyenDanhGiaAsync(int? nguoiDungId, bool laAdmin)
        {
            if (laAdmin) return;

            if (!nguoiDungId.HasValue || !await _nguoiDungPhieuQuyenRepository.AnyAsync(
                    x => x.NguoiDungId == nguoiDungId.Value && x.LoaiPhieu == "PHIEU3" && x.DuocDanhGia))
                throw new ApiException(
                    "Bạn không có quyền tạo Phiếu 3 — liên hệ Admin để được phân quyền \"Đánh giá / nhập liệu\" ở mục Phân quyền theo Phiếu",
                    StatusCodes.Status403Forbidden);
        }

        // Quyền XEM (danh sách + chi tiết) của tài khoản NỘI BỘ — nhà thầu có
        // luật xem riêng (ép lọc theo nhaThauCuaNguoiGoi), không đi qua đây.
        private async Task KiemTraQuyenXemAsync(int? nguoiDungId, bool laAdmin, int? nhaThauCuaNguoiGoi)
        {
            if (laAdmin || nhaThauCuaNguoiGoi.HasValue) return;

            if (!nguoiDungId.HasValue || !await _quyenXemPhieuService.CoQuyenXemAsync(nguoiDungId.Value, "PHIEU3"))
                throw new ApiException(
                    "Bạn không có quyền truy cập Phiếu 3 — liên hệ Admin để được phân quyền ở mục Phân quyền theo Phiếu",
                    StatusCodes.Status403Forbidden);
        }

        public async Task<PagedResultDto<Phieu3BaoCao>> DanhSachAsync(
            int? nhaThauId, int? thang, int? nam, string? trangThai, DateTime? tuNgay, DateTime? denNgay,
            string? tuKhoa, bool chiCuaToi, int page, int pageSize,
            int? nhaThauCuaNguoiGoi, int? nguoiDungId, bool laAdmin)
        {
            await KiemTraQuyenXemAsync(nguoiDungId, laAdmin, nhaThauCuaNguoiGoi);

            var query = _phieuRepository.Query();

            if (nhaThauCuaNguoiGoi.HasValue) query = query.Where(x => x.NhaThauId == nhaThauCuaNguoiGoi);
            else if (nhaThauId.HasValue)     query = query.Where(x => x.NhaThauId == nhaThauId);
            if (thang.HasValue)     query = query.Where(x => x.Thang == thang);
            if (nam.HasValue)       query = query.Where(x => x.Nam == nam);
            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.TrangThai == trangThai);
            // Phiếu 3 là báo cáo THEO THÁNG, không có ngày kiểm tra cụ thể —
            // "khoảng ngày" ở đây lọc theo NGÀY TẠO báo cáo (NgayTao).
            if (tuNgay.HasValue) query = query.Where(x => x.NgayTao >= tuNgay.Value.Date);
            if (denNgay.HasValue) query = query.Where(x => x.NgayTao <= denNgay.Value.Date.AddDays(1).AddTicks(-1));
            if (!string.IsNullOrWhiteSpace(tuKhoa)) query = query.Where(x => x.SoHieu.Contains(tuKhoa));
            if (chiCuaToi && nguoiDungId.HasValue) query = query.Where(x => x.NguoiTao == nguoiDungId.Value);

            var tongSo = query.Count();
            var items = query.OrderByDescending(x => x.Nam)
                        .ThenByDescending(x => x.Thang)
                        .ThenByDescending(x => x.Id)
                        .Skip((page - 1) * pageSize).Take(pageSize)
                        .ToList();
            return new PagedResultDto<Phieu3BaoCao> { Items = items, TotalCount = tongSo, Page = page, PageSize = pageSize };
        }

        // ============================================================
        // CHI TIẾT
        // ============================================================

        public async Task<Phieu3ResponseDto> ChiTietAsync(int id, int? nhaThauCuaNguoiGoi, int? nguoiDungId, bool laAdmin)
        {
            await KiemTraQuyenXemAsync(nguoiDungId, laAdmin, nhaThauCuaNguoiGoi);
            return await LayChiTietAsync(id, nhaThauCuaNguoiGoi);
        }

        // Tách khỏi ChiTietAsync để các thao tác NỘI BỘ tự build lại response
        // DTO sau khi ghi mà không phải soi lại quyền XEM (xem Phieu1Service).
        private async Task<Phieu3ResponseDto> LayChiTietAsync(int id, int? nhaThauCuaNguoiGoi)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);

            if (nhaThauCuaNguoiGoi.HasValue && phieu.NhaThauId != nhaThauCuaNguoiGoi)
                throw new ApiException("Bạn không có quyền xem báo cáo của nhà thầu khác", StatusCodes.Status403Forbidden);

            var bang1Entities = (await _bang1Repository.FindAsync(x => x.PhieuId == id))
                        .OrderBy(x => x.Id).ToList();
            var bang1 = new List<Phieu3Bang1DongDto>();
            foreach (var dong in bang1Entities)
                bang1.Add(await BuildBang1DongDtoAsync(dong));

            var bang2Dong = (await _bang2DongRepository.FindAsync(x => x.PhieuId == id))
                             .OrderBy(x => x.Id).ToList();
            var bang2DongIds = bang2Dong.Select(x => x.Id).ToList();
            var bang2GiaTri = (await _bang2GiaTriRepository.FindAsync(x => bang2DongIds.Contains(x.DongId)))
                               .ToList();

            var bang2 = bang2Dong.Select(d => new Phieu3Bang2DongDto
            {
                Id = d.Id,
                PhongBanId = d.PhongBanId,
                GiaTri = bang2GiaTri.Where(g => g.DongId == d.Id).OrderBy(g => g.MaTieuChi).ToList(),
            }).ToList();

            var yKien = await _yKienRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            var doan = await BuildDoanDtoAsync(id);

            return new Phieu3ResponseDto
            {
                Phieu = phieu,
                Bang1 = bang1,
                Bang2 = bang2,
                YKienNhaThau = yKien,
                Doan = doan,
            };
        }

        // Nạp "đoạn" + địa điểm của phiếu, resolve TuBuaAnCode/DenBuaAnCode để
        // FE hiển thị không cần gọi thêm API BuaAn.
        private async Task<List<DoanDto>> BuildDoanDtoAsync(int phieuId)
        {
            var doanEntities = (await _doanRepository.FindAsync(x => x.PhieuId == phieuId))
                .OrderBy(x => x.TuNgay).ThenBy(x => x.Id).ToList();
            if (doanEntities.Count == 0) return new List<DoanDto>();

            var doanIds = doanEntities.Select(x => x.Id).ToList();
            var diaDiem = await _doanDiaDiemRepository.FindAsync(x => doanIds.Contains(x.DoanId));
            var buaAnTheoId = (await _buaAnRepository.GetAllAsync()).ToDictionary(b => b.ID, b => b.CodeBuaAn);

            return doanEntities.Select(d => new DoanDto
            {
                Id = d.Id,
                TuNgay = d.TuNgay,
                TuBuaAnId = d.TuBuaAnId,
                TuBuaAnCode = buaAnTheoId.TryGetValue(d.TuBuaAnId, out var tuMa) ? tuMa : null,
                DenNgay = d.DenNgay,
                DenBuaAnId = d.DenBuaAnId,
                DenBuaAnCode = buaAnTheoId.TryGetValue(d.DenBuaAnId, out var denMa) ? denMa : null,
                DiaDiemNhaAnIds = diaDiem.Where(x => x.DoanId == d.Id).Select(x => x.DiaDiemNhaAnId).ToList(),
            }).ToList();
        }

        // ============================================================
        // TẠO MỚI
        // ============================================================

        public async Task<Phieu3ResponseDto> ThemAsync(Phieu3Request request, int? nguoiTaoId, bool laAdmin)
        {
            await KiemTraQuyenDanhGiaAsync(nguoiTaoId, laAdmin);

            var nhaThau = await _nhaThauRepository.GetByIdAsync(request.NhaThauId)
                ?? throw new ApiException("Không tìm thấy nhà thầu");

            var daTonTai = await _phieuRepository.AnyAsync(x =>
                x.Thang == request.Thang && x.Nam == request.Nam && x.NhaThauId == request.NhaThauId);
            if (daTonTai)
                throw new ApiException("Đã tồn tại báo cáo cho tháng/nhà thầu này");

            var soHieu = await SinhSoHieuAsync(request.Nam);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var phieu = new Phieu3BaoCao
                {
                    SoHieu    = soHieu,
                    Thang     = request.Thang,
                    Nam       = request.Nam,
                    NhaThauId = request.NhaThauId,
                    NguoiTao  = nguoiTaoId,
                    TrangThai = "NHAP",
                    NgayTao   = DateTime.Now,
                };
                await _phieuRepository.AddAsync(phieu);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật

                await KhoiTaoBang1RongAsync(phieu.Id);
                await KhoiTaoBang2RongAsync(phieu.Id);
                await TaoDoanAsync(phieu.Id, request.Doan);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                // Tính tự động ngay sau khi tạo (Bảng 1: LUOT_CBNV_THAM_GIA + TY_LE_PHAN_TRAM)
                await TinhLaiAsync(phieu.Id);

                return await LayChiTietAsync(phieu.Id, null);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================================
        // SỬA TAY (Bảng 1 + Bảng 2)
        // ============================================================

        public async Task<Phieu3ResponseDto> SuaAsync(int id, Phieu3SuaRequest request, int? nguoiSuaId)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể sửa báo cáo ở trạng thái Nháp hoặc Từ chối");

            // ---- Bảng 1 ----
            var bang1HienTai = await _bang1Repository.FindAsync(x => x.PhieuId == id);
            foreach (var req in request.Bang1)
            {
                var dong = bang1HienTai.FirstOrDefault(x => x.MaDong == req.MaDong);
                if (dong == null) continue; // 3 dòng cố định, không cho thêm dòng lạ

                var coThayDoi =
                    dong.Diem1 != req.Diem1 || dong.Diem2 != req.Diem2 || dong.Diem3 != req.Diem3 ||
                    dong.Diem4 != req.Diem4 || dong.Diem5 != req.Diem5 || dong.Tong != req.Tong;
                if (!coThayDoi) continue;

                await GhiNhatKyNeuDoiAsync("PHIEU3_BANG1", dong.Id, "Diem1", dong.Diem1, req.Diem1, nguoiSuaId);
                await GhiNhatKyNeuDoiAsync("PHIEU3_BANG1", dong.Id, "Diem2", dong.Diem2, req.Diem2, nguoiSuaId);
                await GhiNhatKyNeuDoiAsync("PHIEU3_BANG1", dong.Id, "Diem3", dong.Diem3, req.Diem3, nguoiSuaId);
                await GhiNhatKyNeuDoiAsync("PHIEU3_BANG1", dong.Id, "Diem4", dong.Diem4, req.Diem4, nguoiSuaId);
                await GhiNhatKyNeuDoiAsync("PHIEU3_BANG1", dong.Id, "Diem5", dong.Diem5, req.Diem5, nguoiSuaId);
                await GhiNhatKyNeuDoiAsync("PHIEU3_BANG1", dong.Id, "Tong",  dong.Tong,  req.Tong,  nguoiSuaId);

                dong.Diem1 = req.Diem1;
                dong.Diem2 = req.Diem2;
                dong.Diem3 = req.Diem3;
                dong.Diem4 = req.Diem4;
                dong.Diem5 = req.Diem5;
                dong.Tong  = req.Tong;
                dong.ChinhSuaThuCong = true;
                _bang1Repository.Update(dong);
            }

            // ---- Bảng 2 ----
            var bang2DongHienTai = await _bang2DongRepository.FindAsync(x => x.PhieuId == id);
            foreach (var reqDong in request.Bang2)
            {
                var dong = bang2DongHienTai.FirstOrDefault(x => x.PhongBanId == reqDong.PhongBanId);
                if (dong == null) continue; // 2 dòng cố định theo PhongBan khởi tạo sẵn

                var giaTriHienTai = await _bang2GiaTriRepository.FindAsync(x => x.DongId == dong.Id);
                foreach (var reqGiaTri in reqDong.GiaTri)
                {
                    var oGiaTri = giaTriHienTai.FirstOrDefault(x => x.MaTieuChi == reqGiaTri.MaTieuChi);
                    if (oGiaTri == null) continue; // 6 tiêu chí cố định

                    if (oGiaTri.GiaTri == reqGiaTri.GiaTri) continue;

                    await GhiNhatKyNeuDoiAsync("PHIEU3_BANG2", oGiaTri.Id, reqGiaTri.MaTieuChi, oGiaTri.GiaTri, reqGiaTri.GiaTri, nguoiSuaId);

                    oGiaTri.GiaTri = reqGiaTri.GiaTri;
                    oGiaTri.ChinhSuaThuCong = true;
                    _bang2GiaTriRepository.Update(oGiaTri);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return await LayChiTietAsync(id, null);
        }

        // ============================================================
        // TÍNH LẠI BẢNG 1 TỰ ĐỘNG (từ Phieu2_DanhGia)
        // ============================================================

        public async Task<Phieu3ResponseDto> TinhLaiAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);

            var doanCalc = await LayDoanCalcAsync(id);
            var soLuotTheoMuc = await TinhSoLuotCbnvTheoMucTuDoanAsync(doanCalc);
            var tongLuot = soLuotTheoMuc[1] + soLuotTheoMuc[2] + soLuotTheoMuc[3] + soLuotTheoMuc[4] + soLuotTheoMuc[5];

            var bang1 = await _bang1Repository.FindAsync(x => x.PhieuId == id);

            var dongLuot = bang1.FirstOrDefault(x => x.MaDong == "LUOT_CBNV_THAM_GIA");
            if (dongLuot != null && !dongLuot.ChinhSuaThuCong)
            {
                dongLuot.Diem1 = soLuotTheoMuc[1];
                dongLuot.Diem2 = soLuotTheoMuc[2];
                dongLuot.Diem3 = soLuotTheoMuc[3];
                dongLuot.Diem4 = soLuotTheoMuc[4];
                dongLuot.Diem5 = soLuotTheoMuc[5];
                dongLuot.Tong  = tongLuot;
                dongLuot.NguonDuLieu = "Tự động: đếm KetQuaDanhGia (CBNV tự chấm qua kiosk, theo ID_BuaAn) tại các địa điểm/đoạn thời gian đã khai báo";
                _bang1Repository.Update(dongLuot);
            }

            // Tổng suất ăn tại chỗ — tự động = tổng DuLieuCom.Com_ThucTe_{Sang,
            // Trua,Chieu,Dem} theo đúng biên bữa ăn của từng đoạn đã khai báo
            // (thay cho suy luận "nhà ăn rõ ràng" cũ).
            var dongSuatAn = bang1.FirstOrDefault(x => x.MaDong == "TONG_SUAT_AN");
            if (dongSuatAn != null && !dongSuatAn.ChinhSuaThuCong)
            {
                var tongSuatAn = await TinhTongSuatAnTuDoanAsync(doanCalc);
                dongSuatAn.Tong = tongSuatAn;
                dongSuatAn.NguonDuLieu = "Tự động: tổng DuLieuCom.Com_ThucTe_{Sang,Trua,Chieu,Dem} tại các địa điểm/đoạn thời gian đã khai báo";
                _bang1Repository.Update(dongSuatAn);
            }

            // Tỷ lệ % theo tiêu chí đánh giá = Số lượt CBNV tham gia đánh giá ở
            // TỪNG MỨC / Tổng số lượt CBNV tham gia đánh giá × 100% — xác nhận
            // nghiệp vụ, KHÔNG chia cho "Tổng suất ăn tại chỗ" (dòng đó là 1 chỉ
            // tiêu độc lập, không phải mẫu số của tỷ lệ này).
            var dongTyLe = bang1.FirstOrDefault(x => x.MaDong == "TY_LE_PHAN_TRAM");
            if (dongTyLe != null && !dongTyLe.ChinhSuaThuCong)
            {
                if (tongLuot > 0)
                {
                    dongTyLe.Diem1 = Math.Round(soLuotTheoMuc[1] / tongLuot * 100, 2);
                    dongTyLe.Diem2 = Math.Round(soLuotTheoMuc[2] / tongLuot * 100, 2);
                    dongTyLe.Diem3 = Math.Round(soLuotTheoMuc[3] / tongLuot * 100, 2);
                    dongTyLe.Diem4 = Math.Round(soLuotTheoMuc[4] / tongLuot * 100, 2);
                    dongTyLe.Diem5 = Math.Round(soLuotTheoMuc[5] / tongLuot * 100, 2);
                    dongTyLe.Tong  = 100m;
                }
                else
                {
                    dongTyLe.Diem1 = dongTyLe.Diem2 = dongTyLe.Diem3 = dongTyLe.Diem4 = dongTyLe.Diem5 = dongTyLe.Tong = null;
                }
                dongTyLe.NguonDuLieu = "Tự động = Số lượt CBNV tham gia đánh giá ở từng mức / Tổng số lượt CBNV tham gia đánh giá × 100%";
                _bang1Repository.Update(dongTyLe);
            }

            await TinhLaiBang2Async(phieu);

            await _unitOfWork.SaveChangesAsync();
            return await LayChiTietAsync(id, null);
        }

        // ============================================================
        // TÍNH LẠI BẢNG 2 TỰ ĐỘNG (xác nhận nghiệp vụ 2026-09-01)
        // ============================================================
        //
        // Dòng P.ĐN: TC1,TC2,TC4,TC5,TC6 = trung bình Phieu2_TieuChi.Diem
        // (theo đúng MaTieuChi tương ứng, xem AnhXaTcSangPhieu2) của TẤT CẢ
        // Phiếu 2 nhà thầu này lập trong Tháng/Năm của báo cáo.
        // Dòng P.ATMT: CHỈ TC1 (VSATTP) = trung bình Phieu1_KetLuan.DiemDanhGia
        // của các Phiếu 1 do P.ATMT lập cho nhà thầu này trong Tháng/Năm (Phiếu
        // 1 chỉ đo VSATTP nên không có nguồn cho TC2/4/5/6 của dòng ATMT).
        // TC3 "Đa dạng thực đơn" không có nguồn tự động ở CẢ 2 dòng — luôn
        // nhập tay. Chỉ ghi đè các ô ChinhSuaThuCong = false, giống Bảng 1.
        private async Task TinhLaiBang2Async(Phieu3BaoCao phieu)
        {
            var bang2Dong = await _bang2DongRepository.FindAsync(x => x.PhieuId == phieu.Id);
            if (bang2Dong.Count == 0) return;

            var phongBanCanDung = (await _phongBanRepository.FindAsync(x => MaPhongBanBang2.Contains(x.Ma))).ToList();
            var pbDoiNgoai = phongBanCanDung.FirstOrDefault(x => x.Ma == "PDN");
            var pbAtmt = phongBanCanDung.FirstOrDefault(x => x.Ma == "PATMT");

            // Union ngày của TẤT CẢ đoạn đã khai báo cho phiếu này — thay cho
            // lọc theo Thang/Nam cũ, vì đoạn có thể vượt ranh giới tháng (xác
            // nhận nghiệp vụ). Bảng 2 KHÔNG quan tâm bữa ăn bắt đầu/kết thúc
            // của đoạn, chỉ quan tâm Ngày.
            var doanRanges = await LayDoanRangesAsync(phieu.Id);

            // ---- Dòng P.ĐN ----
            var dongDoiNgoai = pbDoiNgoai != null ? bang2Dong.FirstOrDefault(x => x.PhongBanId == pbDoiNgoai.Id) : null;
            if (dongDoiNgoai != null)
            {
                var giaTriDoiNgoai = await _bang2GiaTriRepository.FindAsync(x => x.DongId == dongDoiNgoai.Id);

                // Chỉ tính từ Phiếu 2 đã DUYỆT — Nháp/Chờ ký chưa phải số liệu
                // chính thức, không được đưa vào trung bình Bảng 2.
                var phieu2CuaThang = (await _phieu2Repository.FindAsync(x =>
                        x.NhaThauId == phieu.NhaThauId && x.TrangThai == "DA_DUYET"))
                    .Where(x => x.ThoiGianTu.HasValue && TrongDoanNao(x.ThoiGianTu.Value.Date, doanRanges))
                    .ToList();
                var phieu2Ids = phieu2CuaThang.Select(x => x.Id).ToHashSet();
                var tieuChiCuaThang = phieu2Ids.Count > 0
                    ? await _phieu2TieuChiRepository.FindAsync(x => phieu2Ids.Contains(x.PhieuId))
                    : new List<Phieu2TieuChi>();

                foreach (var (maTc, maPhieu2) in AnhXaTcSangPhieu2)
                {
                    var oGiaTri = giaTriDoiNgoai.FirstOrDefault(x => x.MaTieuChi == maTc);
                    if (oGiaTri == null || oGiaTri.ChinhSuaThuCong) continue;

                    if (maTc == "TC1")
                    {
                        // VSATTP: Phieu2_TieuChi.Diem đã có sẵn điểm số thật (lấy từ
                        // Phiếu 1 liên kết hoặc nhập tay khi Đạt) -> TB trực tiếp.
                        var cacDiem = tieuChiCuaThang
                            .Where(x => x.MaTieuChi == maPhieu2 && x.Diem.HasValue)
                            .Select(x => x.Diem!.Value)
                            .ToList();
                        oGiaTri.GiaTri = cacDiem.Count > 0 ? Math.Round(cacDiem.Average(), 2) : null;
                        oGiaTri.ThamChieuNguon = $"Tự động: TB Phieu2_TieuChi.Diem ({maPhieu2}) của các Phiếu 2 đã duyệt trong khoảng ngày các đoạn đã khai báo";
                    }
                    else
                    {
                        // Các tiêu chí còn lại (TC2/TC4/TC5/TC6) chỉ có Đạt/Không đạt ở
                        // Phiếu 2, KHÔNG có điểm số -> quy đổi = số Phiếu 2 Đạt tiêu chí
                        // này / số Phiếu 2 CÓ ĐÁNH GIÁ tiêu chí này (Dat hoặc KhongDat) ×
                        // 5 — xác nhận nghiệp vụ 2026-09-14, sửa lại: Phiếu 2 BỎ TRỐNG
                        // (chưa đánh giá) tiêu chí này KHÔNG còn bị tính ngầm là "không
                        // đạt" nữa, loại hẳn khỏi cả tử số lẫn mẫu số.
                        var soPhieuDanhGia = tieuChiCuaThang.Count(x => x.MaTieuChi == maPhieu2 && (x.Dat || x.KhongDat));
                        var soLuongDat = tieuChiCuaThang.Count(x => x.MaTieuChi == maPhieu2 && x.Dat);
                        oGiaTri.GiaTri = soPhieuDanhGia > 0 ? Math.Round((decimal)soLuongDat / soPhieuDanhGia * 5, 2) : null;
                        oGiaTri.ThamChieuNguon = $"Tự động: Số Phiếu 2 Đạt ({maPhieu2}) / Số Phiếu 2 có đánh giá tiêu chí này (đã duyệt, trong khoảng ngày các đoạn đã khai báo) × 5";
                    }
                    _bang2GiaTriRepository.Update(oGiaTri);
                }
            }

            // ---- Dòng P.ATMT (chỉ TC1) ----
            var dongAtmt = pbAtmt != null ? bang2Dong.FirstOrDefault(x => x.PhongBanId == pbAtmt.Id) : null;
            if (dongAtmt != null && pbAtmt != null)
            {
                var giaTriAtmt = await _bang2GiaTriRepository.FindAsync(x => x.DongId == dongAtmt.Id);
                var oTc1 = giaTriAtmt.FirstOrDefault(x => x.MaTieuChi == "TC1");
                if (oTc1 != null && !oTc1.ChinhSuaThuCong)
                {
                    // Chỉ tính từ Phiếu 1 đã DUYỆT — cùng quy tắc với Phiếu 2 ở trên.
                    var phieu1CuaThang = (await _phieu1Repository.FindAsync(x =>
                            x.NhaThauId == phieu.NhaThauId && x.PhongBanId == pbAtmt.Id && x.TrangThai == "DA_DUYET"))
                        .Where(x => TrongDoanNao(x.NgayKiemTra.Date, doanRanges))
                        .ToList();
                    var phieu1Ids = phieu1CuaThang.Select(x => x.Id).ToHashSet();
                    var ketLuanCuaThang = phieu1Ids.Count > 0
                        ? await _phieu1KetLuanRepository.FindAsync(x => phieu1Ids.Contains(x.PhieuId))
                        : new List<Phieu1KetLuan>();

                    var cacDiem = ketLuanCuaThang.Where(x => x.DiemDanhGia.HasValue).Select(x => x.DiemDanhGia!.Value).ToList();

                    oTc1.GiaTri = cacDiem.Count > 0 ? Math.Round(cacDiem.Average(), 2) : null;
                    oTc1.ThamChieuNguon = "Tự động: TB Phieu1_KetLuan.DiemDanhGia của các Phiếu 1 do P.ATMT lập cho nhà thầu, trong khoảng ngày các đoạn đã khai báo";
                    _bang2GiaTriRepository.Update(oTc1);
                }
            }
        }

        // ============================================================
        // XÓA
        // ============================================================

        // laAdmin bypass ràng buộc trạng thái — xem Phieu1Service.XoaAsync.
        public async Task XoaAsync(int id, bool laAdmin)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);
            if (!laAdmin && phieu.TrangThai != "NHAP")
                throw new ApiException("Chỉ có thể xóa báo cáo ở trạng thái Nháp");

            var bang1 = await _bang1Repository.FindAsync(x => x.PhieuId == id);
            _bang1Repository.RemoveRange(bang1);

            var bang2Dong = await _bang2DongRepository.FindAsync(x => x.PhieuId == id);
            var bang2DongIds = bang2Dong.Select(x => x.Id).ToList();
            var bang2GiaTri = await _bang2GiaTriRepository.FindAsync(x => bang2DongIds.Contains(x.DongId));
            _bang2GiaTriRepository.RemoveRange(bang2GiaTri);
            _bang2DongRepository.RemoveRange(bang2Dong);

            var yKien = await _yKienRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            if (yKien != null) _yKienRepository.Remove(yKien);

            var doan = await _doanRepository.FindAsync(x => x.PhieuId == id);
            var doanIds = doan.Select(x => x.Id).ToList();
            if (doanIds.Count > 0)
            {
                var doanDiaDiem = await _doanDiaDiemRepository.FindAsync(x => doanIds.Contains(x.DoanId));
                _doanDiaDiemRepository.RemoveRange(doanDiaDiem);
            }
            _doanRepository.RemoveRange(doan);

            var chuKy = await _chuKyPhieuRepository.FindAsync(x => x.LoaiDoiTuong == "PHIEU3" && x.DoiTuongId == id);
            _chuKyPhieuRepository.RemoveRange(chuKy);

            _phieuRepository.Remove(phieu);
            await _unitOfWork.SaveChangesAsync();
        }

        // ============================================================
        // THÊM / XÓA ĐOẠN THỜI GIAN (chỉ khi NHAP/TU_CHOI) — thao tác xong
        // tính lại Bảng 1 ngay, cùng pattern ThemNhaThauAsync/XoaNhaThauAsync
        // của Phiếu 4.
        // ============================================================

        public async Task<Phieu3ResponseDto> ThemDoanAsync(int id, DoanRequest request)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể sửa đoạn khi báo cáo ở trạng thái Nháp hoặc Từ chối");

            var buaAnThuTu = await LayBuaAnThuTuAsync();
            KiemTraDoanHopLe(request, buaAnThuTu);

            var doan = new Phieu3Doan
            {
                PhieuId = id,
                TuNgay = request.TuNgay.Date,
                TuBuaAnId = request.TuBuaAnId,
                DenNgay = request.DenNgay.Date,
                DenBuaAnId = request.DenBuaAnId,
            };
            await _doanRepository.AddAsync(doan);
            await _unitOfWork.SaveChangesAsync();
            foreach (var diaDiemId in request.DiaDiemNhaAnIds.Distinct())
                await _doanDiaDiemRepository.AddAsync(new Phieu3DoanDiaDiem { DoanId = doan.Id, DiaDiemNhaAnId = diaDiemId });
            await _unitOfWork.SaveChangesAsync();

            return await TinhLaiAsync(id);
        }

        public async Task<Phieu3ResponseDto> XoaDoanAsync(int id, int doanId)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể sửa đoạn khi báo cáo ở trạng thái Nháp hoặc Từ chối");

            var doan = await _doanRepository.GetByIdAsync(doanId);
            if (doan == null || doan.PhieuId != id)
                throw new ApiException("Không tìm thấy đoạn", StatusCodes.Status404NotFound);

            var diaDiem = await _doanDiaDiemRepository.FindAsync(x => x.DoanId == doanId);
            _doanDiaDiemRepository.RemoveRange(diaDiem);
            _doanRepository.Remove(doan);
            await _unitOfWork.SaveChangesAsync();

            return await TinhLaiAsync(id);
        }

        // ============================================================
        // GỬI KÝ / ĐỒNG BỘ TRẠNG THÁI
        // ============================================================

        public async Task<Phieu3BaoCao> GuiKyAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Báo cáo không ở trạng thái phù hợp để gửi ký");

            await _chuKyPhieuService.KhoiTaoLuongKyAsync("PHIEU3", id);
            phieu.TrangThai = "CHO_KY";
            _phieuRepository.Update(phieu);
            await _unitOfWork.SaveChangesAsync();
            return phieu;
        }

        public async Task<Phieu3BaoCao> DongBoTrangThaiAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);

            var trangThaiMoi = await _chuKyPhieuService.TrangThaiTongAsync("PHIEU3", id);
            if (trangThaiMoi is "DA_DUYET" or "TU_CHOI" or "CHO_KY")
            {
                phieu.TrangThai = trangThaiMoi;
                _phieuRepository.Update(phieu);
                await _unitOfWork.SaveChangesAsync();
            }
            return phieu;
        }

        // ============================================================
        // Ý KIẾN NHÀ THẦU
        // ============================================================

        public async Task<Phieu3ResponseDto> PhanHoiYKienNhaThauAsync(int id, Phieu3YKienNhaThauRequest request)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);

            var yKien = await _yKienRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            if (yKien == null)
            {
                yKien = new Phieu3YKienNhaThau { PhieuId = id };
                await _yKienRepository.AddAsync(yKien);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật trước khi Update
            }
            yKien.YKien = request.YKien;
            _yKienRepository.Update(yKien);
            await _unitOfWork.SaveChangesAsync();

            return await LayChiTietAsync(id, null);
        }

        // ============================================================
        // ĐOẠN THỜI GIAN — helper dùng chung cho Bảng 1 (thay thế hoàn toàn
        // suy luận "nhà ăn rõ ràng" cũ)
        // ============================================================

        // Model tính toán nội bộ (không lưu DB) dựng từ Phieu3Doan +
        // Phieu3DoanDiaDiem — TuThuTu/DenThuTu là thứ tự bữa ăn 1..4 (Sáng..Đêm).
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

        private async Task<List<DoanCalc>> LayDoanCalcAsync(int phieuId)
        {
            var doanEntities = await _doanRepository.FindAsync(x => x.PhieuId == phieuId);
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

        private async Task<List<(DateTime TuNgay, DateTime DenNgay)>> LayDoanRangesAsync(int phieuId)
        {
            var doan = await _doanRepository.FindAsync(x => x.PhieuId == phieuId);
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

        private static void KiemTraDoanHopLe(DoanRequest req, Dictionary<int, int> buaAnThuTu)
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
        }

        // Tạo các đoạn khai báo lúc lập phiếu — dùng trong ThemAsync, bên
        // trong transaction (mỗi đoạn cần Id thật trước khi thêm địa điểm con).
        private async Task TaoDoanAsync(int phieuId, List<DoanRequest> danhSachDoan)
        {
            if (danhSachDoan.Count == 0) return;
            var buaAnThuTu = await LayBuaAnThuTuAsync();

            foreach (var req in danhSachDoan)
            {
                KiemTraDoanHopLe(req, buaAnThuTu);
                var doan = new Phieu3Doan
                {
                    PhieuId = phieuId,
                    TuNgay = req.TuNgay.Date,
                    TuBuaAnId = req.TuBuaAnId,
                    DenNgay = req.DenNgay.Date,
                    DenBuaAnId = req.DenBuaAnId,
                };
                await _doanRepository.AddAsync(doan);
                await _unitOfWork.SaveChangesAsync();
                foreach (var diaDiemId in req.DiaDiemNhaAnIds.Distinct())
                    await _doanDiaDiemRepository.AddAsync(new Phieu3DoanDiaDiem { DoanId = doan.Id, DiaDiemNhaAnId = diaDiemId });
            }
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
        // (thay thế hoàn toàn suy luận "nhà ăn rõ ràng" cũ).
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
        // HELPERS
        // ============================================================

        // Quy ước đánh số biên bản Phiếu 3 (Báo cáo chất lượng dịch vụ suất ăn):
        // {seq:003}/{năm}/BCCLDVSA-P.ĐN — 1 dãy số DUY NHẤT dùng chung cho toàn
        // bộ nhà thầu, bắt đầu từ 001 ngày 1/1 và reset lại vào ngày 1/1 năm sau
        // (KHÔNG tách theo nhà thầu) — xem mục 5.3 PhanTichNghiepVu.md.
        private async Task<string> SinhSoHieuAsync(int nam)
        {
            var seq = await _soHieuService.SinhSoTiepTheoAsync("PHIEU3", null, nam, null);
            return $"{seq:D3}/{nam}/BCCLDVSA-P.ĐN";
        }

        private async Task KhoiTaoBang1RongAsync(int phieuId)
        {
            foreach (var mau in Bang1DongCoDinh)
            {
                await _bang1Repository.AddAsync(new Phieu3Bang1Dong
                {
                    PhieuId = phieuId,
                    MaDong  = mau.Ma,
                    TenDong = mau.Ten,
                    ChinhSuaThuCong = false,
                    NguonDuLieu = "Chưa tính",
                });
            }
        }

        private async Task KhoiTaoBang2RongAsync(int phieuId)
        {
            var phongBanCanDung = (await _phongBanRepository.FindAsync(x => MaPhongBanBang2.Contains(x.Ma)))
                                   .ToList();

            foreach (var maPb in MaPhongBanBang2)
            {
                var pb = phongBanCanDung.FirstOrDefault(x => x.Ma == maPb);
                if (pb == null) continue; // phòng ban chưa được cấu hình trong master data

                var dong = new Phieu3Bang2Dong { PhieuId = phieuId, PhongBanId = pb.Id };
                await _bang2DongRepository.AddAsync(dong);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật cho giá trị con

                foreach (var maTc in MaTieuChiBang2)
                {
                    await _bang2GiaTriRepository.AddAsync(new Phieu3Bang2GiaTri
                    {
                        DongId = dong.Id,
                        MaTieuChi = maTc,
                        ChinhSuaThuCong = false,
                        ThamChieuNguon = "Nhập tay — chưa có công thức tự động",
                    });
                }
            }
        }

        private async Task GhiNhatKyNeuDoiAsync(string loaiDoiTuong, int doiTuongId, string tenTruong, decimal? giaTriCu, decimal? giaTriMoi, int? nguoiSuaId)
        {
            if (giaTriCu == giaTriMoi) return;
            await _nhatKyChinhSuaService.GhiAsync(loaiDoiTuong, doiTuongId, tenTruong, giaTriCu?.ToString(), giaTriMoi?.ToString(), nguoiSuaId);
        }

        // Ghép Phieu3Bang1Dong (entity) với lịch sử NhatKyChinhSua để biết
        // CHÍNH XÁC ô nào (diem1..diem5, tong) đang lệch giá trị hệ thống —
        // ChinhSuaThuCong của entity chỉ là cờ cấp DÒNG, không phân biệt được
        // ô. Giá trị hệ thống gốc của 1 ô = GiaTriCu của bản ghi log SỚM NHẤT
        // cho ô đó (ghi ngay lần đầu người dùng sửa, trước đó là số tự tính từ
        // TinhLaiAsync). Chỉ đánh dấu "đã sửa tay" (tô vàng) khi giá trị HIỆN
        // TẠI còn khác giá trị hệ thống gốc — nếu người dùng sửa rồi sửa lại
        // đúng bằng số hệ thống thì hết lệch, KHÔNG tô vàng nữa (dù lịch sử
        // sửa vẫn còn lưu để đối chiếu/audit).
        private async Task<Phieu3Bang1DongDto> BuildBang1DongDtoAsync(Phieu3Bang1Dong dong)
        {
            var lichSu = await _nhatKyChinhSuaService.LayTheoDoiTuongAsync("PHIEU3_BANG1", dong.Id);

            decimal? HeThongCua(string tenTruong)
            {
                var somNhat = lichSu
                    .Where(x => string.Equals(x.TenTruong, tenTruong, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.NgayThayDoi)
                    .FirstOrDefault();
                return somNhat != null && decimal.TryParse(somNhat.GiaTriCu, out var giaTri) ? giaTri : null;
            }

            var tungO = new (string Khoa, string TenTruong, decimal? GiaTriHienTai)[]
            {
                ("diem1", "Diem1", dong.Diem1),
                ("diem2", "Diem2", dong.Diem2),
                ("diem3", "Diem3", dong.Diem3),
                ("diem4", "Diem4", dong.Diem4),
                ("diem5", "Diem5", dong.Diem5),
                ("tong",  "Tong",  dong.Tong),
            };

            var giaTriHeThong = tungO.ToDictionary(o => o.Khoa, o => HeThongCua(o.TenTruong));
            var coLichSu = lichSu.Select(x => x.TenTruong?.ToLowerInvariant()).ToHashSet();
            var truongDaSuaTay = tungO
                .Where(o => coLichSu.Contains(o.Khoa) && o.GiaTriHienTai != giaTriHeThong[o.Khoa])
                .Select(o => o.Khoa)
                .ToList();

            return new Phieu3Bang1DongDto
            {
                Id = dong.Id,
                PhieuId = dong.PhieuId,
                MaDong = dong.MaDong,
                TenDong = dong.TenDong,
                Diem1 = dong.Diem1,
                Diem2 = dong.Diem2,
                Diem3 = dong.Diem3,
                Diem4 = dong.Diem4,
                Diem5 = dong.Diem5,
                Tong = dong.Tong,
                ChinhSuaThuCong = dong.ChinhSuaThuCong,
                NguonDuLieu = dong.NguonDuLieu,
                TruongDaSuaTay = truongDaSuaTay,
                Diem1HeThong = truongDaSuaTay.Contains("diem1") ? giaTriHeThong["diem1"] : null,
                Diem2HeThong = truongDaSuaTay.Contains("diem2") ? giaTriHeThong["diem2"] : null,
                Diem3HeThong = truongDaSuaTay.Contains("diem3") ? giaTriHeThong["diem3"] : null,
                Diem4HeThong = truongDaSuaTay.Contains("diem4") ? giaTriHeThong["diem4"] : null,
                Diem5HeThong = truongDaSuaTay.Contains("diem5") ? giaTriHeThong["diem5"] : null,
                TongHeThong = truongDaSuaTay.Contains("tong") ? giaTriHeThong["tong"] : null,
            };
        }
    }
}
