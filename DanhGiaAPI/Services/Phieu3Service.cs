using DanhGiaAPI.Common;
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
        private readonly IPhieu2DanhGiaRepository       _phieu2Repository;
        private readonly IPhieu2TieuChiRepository       _phieu2TieuChiRepository;
        private readonly IPhieu2NhaAnRepository         _phieu2NhaAnRepository;
        private readonly IPhieu1KiemTraRepository       _phieu1Repository;
        private readonly IPhieu1KetLuanRepository       _phieu1KetLuanRepository;
        private readonly IKetQuaDanhGiaRepository       _ketQuaDanhGiaRepository;
        private readonly IDuLieuComRepository           _duLieuComRepository;
        private readonly INhaThauRepository             _nhaThauRepository;
        private readonly IPhongBanRepository            _phongBanRepository;
        private readonly ISoHieuService                 _soHieuService;
        private readonly IChuKyPhieuService              _chuKyPhieuService;
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
            IPhieu2DanhGiaRepository       phieu2Repository,
            IPhieu2TieuChiRepository       phieu2TieuChiRepository,
            IPhieu2NhaAnRepository         phieu2NhaAnRepository,
            IPhieu1KiemTraRepository       phieu1Repository,
            IPhieu1KetLuanRepository       phieu1KetLuanRepository,
            IKetQuaDanhGiaRepository       ketQuaDanhGiaRepository,
            IDuLieuComRepository           duLieuComRepository,
            INhaThauRepository             nhaThauRepository,
            IPhongBanRepository            phongBanRepository,
            ISoHieuService                 soHieuService,
            IChuKyPhieuService              chuKyPhieuService,
            INhatKyChinhSuaService          nhatKyChinhSuaService,
            IUnitOfWork                     unitOfWork)
        {
            _phieuRepository        = phieuRepository;
            _bang1Repository        = bang1Repository;
            _bang2DongRepository    = bang2DongRepository;
            _bang2GiaTriRepository  = bang2GiaTriRepository;
            _yKienRepository        = yKienRepository;
            _phieu2Repository       = phieu2Repository;
            _phieu2TieuChiRepository = phieu2TieuChiRepository;
            _phieu2NhaAnRepository  = phieu2NhaAnRepository;
            _phieu1Repository       = phieu1Repository;
            _phieu1KetLuanRepository = phieu1KetLuanRepository;
            _ketQuaDanhGiaRepository = ketQuaDanhGiaRepository;
            _duLieuComRepository    = duLieuComRepository;
            _nhaThauRepository      = nhaThauRepository;
            _phongBanRepository     = phongBanRepository;
            _soHieuService          = soHieuService;
            _chuKyPhieuService      = chuKyPhieuService;
            _nhatKyChinhSuaService  = nhatKyChinhSuaService;
            _unitOfWork             = unitOfWork;
        }

        // ============================================================
        // DANH SÁCH
        // ============================================================

        public async Task<List<Phieu3BaoCao>> DanhSachAsync(int? nhaThauId, int? thang, int? nam, string? trangThai, int? nhaThauCuaNguoiGoi)
        {
            var query = _phieuRepository.Query();

            if (nhaThauCuaNguoiGoi.HasValue) query = query.Where(x => x.NhaThauId == nhaThauCuaNguoiGoi);
            else if (nhaThauId.HasValue)     query = query.Where(x => x.NhaThauId == nhaThauId);
            if (thang.HasValue)     query = query.Where(x => x.Thang == thang);
            if (nam.HasValue)       query = query.Where(x => x.Nam == nam);
            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.TrangThai == trangThai);

            return query.OrderByDescending(x => x.Nam)
                        .ThenByDescending(x => x.Thang)
                        .ThenByDescending(x => x.Id)
                        .ToList();
        }

        // ============================================================
        // CHI TIẾT
        // ============================================================

        public async Task<Phieu3ResponseDto> ChiTietAsync(int id, int? nhaThauCuaNguoiGoi)
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

            return new Phieu3ResponseDto
            {
                Phieu = phieu,
                Bang1 = bang1,
                Bang2 = bang2,
                YKienNhaThau = yKien,
            };
        }

        // ============================================================
        // TẠO MỚI
        // ============================================================

        public async Task<Phieu3ResponseDto> ThemAsync(Phieu3Request request, int? nguoiTaoId)
        {
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
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                // Tính tự động ngay sau khi tạo (Bảng 1: LUOT_CBNV_THAM_GIA + TY_LE_PHAN_TRAM)
                await TinhLaiAsync(phieu.Id);

                return await ChiTietAsync(phieu.Id, null);
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
            return await ChiTietAsync(id, null);
        }

        // ============================================================
        // TÍNH LẠI BẢNG 1 TỰ ĐỘNG (từ Phieu2_DanhGia)
        // ============================================================

        public async Task<Phieu3ResponseDto> TinhLaiAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);

            var danhSachPhieu2 = await _phieu2Repository.FindAsync(x =>
                x.NhaThauId == phieu.NhaThauId && x.Thang == phieu.Thang && x.Nam == phieu.Nam);

            var nhaAnRoRang = await LayNhaAnRoRangAsync(danhSachPhieu2, phieu.Thang, phieu.Nam);
            var soLuotTheoMuc = await TinhSoLuotCbnvTheoMucAsync(nhaAnRoRang, phieu.Thang, phieu.Nam);
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
                dongLuot.NguonDuLieu = $"Tự động: đếm KetQuaDanhGia (CBNV tự chấm qua kiosk) tại các Nhà ăn nhà thầu phụ trách, suy ra qua Phieu2_NhaAn (Tháng {phieu.Thang}/{phieu.Nam}, NhaThauId={phieu.NhaThauId})";
                _bang1Repository.Update(dongLuot);
            }

            // Tổng suất ăn tại chỗ — tự động = tổng DuLieuCom.Com_ThucTe_ALL
            // trong tháng, tại các Nhà ăn nhà thầu phụ trách (cùng tập
            // "nhà ăn rõ ràng" dùng cho LUOT_CBNV_THAM_GIA).
            var dongSuatAn = bang1.FirstOrDefault(x => x.MaDong == "TONG_SUAT_AN");
            if (dongSuatAn != null && !dongSuatAn.ChinhSuaThuCong)
            {
                var tongSuatAn = await TinhTongSuatAnAsync(nhaAnRoRang, phieu.Thang, phieu.Nam);
                dongSuatAn.Tong = tongSuatAn;
                dongSuatAn.NguonDuLieu = $"Tự động: tổng DuLieuCom.Com_ThucTe_ALL tại các Nhà ăn nhà thầu phụ trách, suy ra qua Phieu2_NhaAn (Tháng {phieu.Thang}/{phieu.Nam}, NhaThauId={phieu.NhaThauId})";
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
            return await ChiTietAsync(id, null);
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

            var tuNgay = new DateTime(phieu.Nam, phieu.Thang, 1);
            var denNgay = tuNgay.AddMonths(1).AddDays(-1);

            // ---- Dòng P.ĐN ----
            var dongDoiNgoai = pbDoiNgoai != null ? bang2Dong.FirstOrDefault(x => x.PhongBanId == pbDoiNgoai.Id) : null;
            if (dongDoiNgoai != null)
            {
                var giaTriDoiNgoai = await _bang2GiaTriRepository.FindAsync(x => x.DongId == dongDoiNgoai.Id);

                var phieu2CuaThang = await _phieu2Repository.FindAsync(x =>
                    x.NhaThauId == phieu.NhaThauId && x.Thang == phieu.Thang && x.Nam == phieu.Nam);
                var phieu2Ids = phieu2CuaThang.Select(x => x.Id).ToHashSet();
                var tieuChiCuaThang = phieu2Ids.Count > 0
                    ? await _phieu2TieuChiRepository.FindAsync(x => phieu2Ids.Contains(x.PhieuId))
                    : new List<Phieu2TieuChi>();

                foreach (var (maTc, maPhieu2) in AnhXaTcSangPhieu2)
                {
                    var oGiaTri = giaTriDoiNgoai.FirstOrDefault(x => x.MaTieuChi == maTc);
                    if (oGiaTri == null || oGiaTri.ChinhSuaThuCong) continue;

                    var cacDiem = tieuChiCuaThang
                        .Where(x => x.MaTieuChi == maPhieu2 && x.Diem.HasValue)
                        .Select(x => x.Diem!.Value)
                        .ToList();

                    oGiaTri.GiaTri = cacDiem.Count > 0 ? Math.Round(cacDiem.Average(), 2) : null;
                    oGiaTri.ThamChieuNguon = $"Tự động: TB Phieu2_TieuChi.Diem ({maPhieu2}) của các Phiếu 2 nhà thầu Tháng {phieu.Thang}/{phieu.Nam}";
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
                    var phieu1CuaThang = await _phieu1Repository.FindAsync(x =>
                        x.NhaThauId == phieu.NhaThauId && x.PhongBanId == pbAtmt.Id &&
                        x.NgayKiemTra >= tuNgay && x.NgayKiemTra <= denNgay);
                    var phieu1Ids = phieu1CuaThang.Select(x => x.Id).ToHashSet();
                    var ketLuanCuaThang = phieu1Ids.Count > 0
                        ? await _phieu1KetLuanRepository.FindAsync(x => phieu1Ids.Contains(x.PhieuId))
                        : new List<Phieu1KetLuan>();

                    var cacDiem = ketLuanCuaThang.Where(x => x.DiemDanhGia.HasValue).Select(x => x.DiemDanhGia!.Value).ToList();

                    oTc1.GiaTri = cacDiem.Count > 0 ? Math.Round(cacDiem.Average(), 2) : null;
                    oTc1.ThamChieuNguon = $"Tự động: TB Phieu1_KetLuan.DiemDanhGia của các Phiếu 1 do P.ATMT lập cho nhà thầu Tháng {phieu.Thang}/{phieu.Nam}";
                    _bang2GiaTriRepository.Update(oTc1);
                }
            }
        }

        // ============================================================
        // XÓA
        // ============================================================

        public async Task XoaAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP")
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

            _phieuRepository.Remove(phieu);
            await _unitOfWork.SaveChangesAsync();
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

            return await ChiTietAsync(id, null);
        }

        // ============================================================
        // TÍNH SỐ LƯỢT ĐÁNH GIÁ CBNV (MỨC 1-5) TỪ KETQUADANHGIA (HỆ KIOSK CŨ)
        // ============================================================
        //
        // Suy ra nhà thầu phụ trách 1 Nhà ăn (DiaDiemNhaAn) theo tháng qua
        // Phieu2_NhaAn (tham chiếu logic tới DiaDiemNhaAn.ID — xác
        // nhận nghiệp vụ 2026-08-27, xem modules/Phieu2_DanhGiaSuatAn.md).
        // Thay cho proxy cũ (đếm Phieu2_KetQua.SoTieuChiDat — số tiêu chí
        // "Đạt" trong checklist người đánh giá, không phải dữ liệu CBNV tự
        // chấm) — xác nhận nghiệp vụ 2026-08-28.
        //
        // Quy tắc quy Nhà ăn về nhà thầu: Nhà ăn nào có Phiếu 2 của NHIỀU nhà
        // thầu khác nhau trong cùng tháng thì bị LOẠI khỏi tính tự động cho
        // TẤT CẢ nhà thầu (không đủ căn cứ quy về 1 bên, tránh đếm trùng/gán
        // nhầm). Nhà ăn không có Phiếu 2 nào trong tháng cũng không tính được
        // (không suy ra được thuộc nhà thầu nào).
        // Suy ra tập DiaDiemNhaAnId "rõ ràng" (không dùng chung bởi nhiều nhà
        // thầu khác nhau) mà 1 nhà thầu phụ trách trong tháng, qua
        // Phieu2_NhaAn — dùng chung cho cả LUOT_CBNV_THAM_GIA lẫn TONG_SUAT_AN
        // (xem quy tắc loại trừ ở comment trên TinhSoLuotCbnvTheoMucAsync).
        private async Task<HashSet<int>> LayNhaAnRoRangAsync(
            List<Phieu2DanhGia> danhSachPhieu2CuaNhaThau, int thang, int nam)
        {
            // 1 Phiếu 2 có thể gộp NHIỀU nhà ăn (Phieu2_NhaAn, xem
            // Entities/Phieu2NhaAn.cs) — suy ra tập Nhà ăn của nhà thầu này qua
            // TẤT CẢ nhà ăn nằm trong các Phiếu 2 của họ trong tháng.
            var phieuIdsCuaNhaThau = danhSachPhieu2CuaNhaThau.Select(x => x.Id).ToList();
            var lienKetCuaNhaThau = phieuIdsCuaNhaThau.Count > 0
                ? await _phieu2NhaAnRepository.FindAsync(x => phieuIdsCuaNhaThau.Contains(x.PhieuId))
                : new List<Phieu2NhaAn>();
            var nhaAnCuaNhaThau = lienKetCuaNhaThau.Select(x => x.NhaAnId).Distinct().ToList();
            if (nhaAnCuaNhaThau.Count == 0) return new HashSet<int>();

            // Tất cả Phiếu 2 (MỌI nhà thầu) trong tháng, để xét "nhà ăn rõ ràng"
            var phieu2CungThang = await _phieu2Repository.FindAsync(x => x.Thang == thang && x.Nam == nam);
            var phieu2CungThangIds = phieu2CungThang.Select(x => x.Id).ToList();
            var nhaThauCuaPhieu = phieu2CungThang.ToDictionary(x => x.Id, x => x.NhaThauId);

            var lienKetCungThang = phieu2CungThangIds.Count > 0
                ? await _phieu2NhaAnRepository.FindAsync(x =>
                    phieu2CungThangIds.Contains(x.PhieuId) && nhaAnCuaNhaThau.Contains(x.NhaAnId))
                : new List<Phieu2NhaAn>();

            return nhaAnCuaNhaThau
                .Where(nhaAnId => lienKetCungThang
                    .Where(l => l.NhaAnId == nhaAnId)
                    .Select(l => nhaThauCuaPhieu[l.PhieuId])
                    .Distinct()
                    .Count() == 1)
                .ToHashSet();
        }

        private async Task<decimal[]> TinhSoLuotCbnvTheoMucAsync(HashSet<int> nhaAnRoRang, int thang, int nam)
        {
            var soLuot = new decimal[6]; // [0] không dùng, [1..5]
            if (nhaAnRoRang.Count == 0) return soLuot;

            var tuNgay = new DateTime(nam, thang, 1);
            var denNgay = tuNgay.AddMonths(1).AddDays(-1);

            var ketQuaCbnv = await _ketQuaDanhGiaRepository.FindAsync(x =>
                nhaAnRoRang.Contains(x.DiaDiem_ID) &&
                x.ThoiGianDanhGia.Date >= tuNgay &&
                x.ThoiGianDanhGia.Date <= denNgay);

            foreach (var kq in ketQuaCbnv)
            {
                if (kq.DiemDanhGia is >= 1 and <= 5) soLuot[kq.DiemDanhGia]++;
            }
            return soLuot;
        }

        // ============================================================
        // TÍNH TỔNG SUẤT ĂN TỪ DULIEUCOM (HỆ ĐĂNG KÝ CƠM CŨ)
        // ============================================================
        //
        // Tổng DuLieuCom.Com_ThucTe_ALL trong tháng, tại các Nhà ăn "rõ ràng"
        // (ID_DiemAn = DiaDiemNhaAn.ID) mà nhà thầu này phụ trách — cùng tập
        // nhaAnRoRang dùng cho TinhSoLuotCbnvTheoMucAsync (xem LayNhaAnRoRangAsync).
        private async Task<int> TinhTongSuatAnAsync(HashSet<int> nhaAnRoRang, int thang, int nam)
        {
            if (nhaAnRoRang.Count == 0) return 0;

            var tuNgay = new DateTime(nam, thang, 1);
            var denNgay = tuNgay.AddMonths(1).AddDays(-1);

            var duLieuCom = await _duLieuComRepository.FindAsync(x =>
                nhaAnRoRang.Contains(x.ID_DiemAn) &&
                x.Ngay >= tuNgay && x.Ngay <= denNgay);

            return duLieuCom.Sum(x => x.Com_ThucTe_ALL ?? 0);
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
