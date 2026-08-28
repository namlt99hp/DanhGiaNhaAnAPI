using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Phieu3;
using DanhGiaAPI.Entities;
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
    //     trách trong tháng, suy ra qua Phieu2_DanhGia.NhaAnId. Trước đây dùng
    //     proxy Phieu2_KetQua.SoTieuChiDat (đếm số tiêu chí "Đạt" trong
    //     checklist người đánh giá — không phải dữ liệu CBNV tự chấm thật).
    //   - TONG_SUAT_AN: NHẬP TAY hoàn toàn — Phiếu 1/2 không lưu số suất ăn thực
    //     tế. TinhLaiAsync không bao giờ đụng tới dòng này.
    //   - TY_LE_PHAN_TRAM: TỰ ĐỘNG (suy ra) = LUOT_CBNV_THAM_GIA / TONG_SUAT_AN,
    //     chỉ tính được khi TONG_SUAT_AN.Tong đã được nhập tay và > 0.
    //
    // Bảng 2 (2 dòng P.ĐN/P.ATMT x 6 tiêu chí TC1..TC6): NHẬP TAY HOÀN TOÀN — nội
    // dung/công thức từng tiêu chí chưa được xác nhận nghiệp vụ, TinhLaiAsync
    // không đụng tới Bảng 2.
    //
    // Quy tắc "tính lại": chỉ ghi đè các dòng Bảng 1 có ChinhSuaThuCong = false
    // (chưa từng bị sửa tay) — giữ nguyên giá trị người dùng đã tự nhập/sửa.
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
        private readonly IKetQuaDanhGiaRepository       _ketQuaDanhGiaRepository;
        private readonly INhaThauRepository             _nhaThauRepository;
        private readonly IPhongBanRepository            _phongBanRepository;
        private readonly ISoHieuService                 _soHieuService;
        private readonly IChuKyPhieuService              _chuKyPhieuService;
        private readonly INhatKyChinhSuaService          _nhatKyChinhSuaService;
        private readonly IUnitOfWork                     _unitOfWork;

        public Phieu3Service(
            IPhieu3BaoCaoRepository       phieuRepository,
            IPhieu3Bang1DongRepository     bang1Repository,
            IPhieu3Bang2DongRepository     bang2DongRepository,
            IPhieu3Bang2GiaTriRepository   bang2GiaTriRepository,
            IPhieu3YKienNhaThauRepository  yKienRepository,
            IPhieu2DanhGiaRepository       phieu2Repository,
            IKetQuaDanhGiaRepository       ketQuaDanhGiaRepository,
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
            _ketQuaDanhGiaRepository = ketQuaDanhGiaRepository;
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

        public async Task<List<Phieu3BaoCao>> DanhSachAsync(int? nhaThauId, int? thang, int? nam, string? trangThai)
        {
            var query = _phieuRepository.Query();

            if (nhaThauId.HasValue) query = query.Where(x => x.NhaThauId == nhaThauId);
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

        public async Task<Phieu3ResponseDto> ChiTietAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy báo cáo", StatusCodes.Status404NotFound);

            var bang1 = (await _bang1Repository.FindAsync(x => x.PhieuId == id))
                        .OrderBy(x => x.Id).ToList();

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

            var soHieu = await SinhSoHieuAsync(nhaThau.Ma, request.Nam);

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

                return await ChiTietAsync(phieu.Id);
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
            return await ChiTietAsync(id);
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

            var soLuotTheoMuc = await TinhSoLuotCbnvTheoMucAsync(danhSachPhieu2, phieu.Thang, phieu.Nam);
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
                dongLuot.NguonDuLieu = $"Tự động: đếm KetQuaDanhGia (CBNV tự chấm qua kiosk) tại các Nhà ăn nhà thầu phụ trách, suy ra qua Phieu2_DanhGia.NhaAnId (Tháng {phieu.Thang}/{phieu.Nam}, NhaThauId={phieu.NhaThauId})";
                _bang1Repository.Update(dongLuot);
            }

            var dongTongSuatAn = bang1.FirstOrDefault(x => x.MaDong == "TONG_SUAT_AN");
            var tongSuatAn = dongTongSuatAn?.Tong;

            var dongTyLe = bang1.FirstOrDefault(x => x.MaDong == "TY_LE_PHAN_TRAM");
            if (dongTyLe != null && !dongTyLe.ChinhSuaThuCong)
            {
                if (tongSuatAn.HasValue && tongSuatAn.Value > 0)
                {
                    dongTyLe.Diem1 = Math.Round(soLuotTheoMuc[1] / tongSuatAn.Value * 100, 2);
                    dongTyLe.Diem2 = Math.Round(soLuotTheoMuc[2] / tongSuatAn.Value * 100, 2);
                    dongTyLe.Diem3 = Math.Round(soLuotTheoMuc[3] / tongSuatAn.Value * 100, 2);
                    dongTyLe.Diem4 = Math.Round(soLuotTheoMuc[4] / tongSuatAn.Value * 100, 2);
                    dongTyLe.Diem5 = Math.Round(soLuotTheoMuc[5] / tongSuatAn.Value * 100, 2);
                    dongTyLe.Tong  = Math.Round(tongLuot / tongSuatAn.Value * 100, 2);
                }
                else
                {
                    dongTyLe.Diem1 = dongTyLe.Diem2 = dongTyLe.Diem3 = dongTyLe.Diem4 = dongTyLe.Diem5 = dongTyLe.Tong = null;
                }
                dongTyLe.NguonDuLieu = "Tự động = Lượt CBNV tham gia đánh giá / Tổng suất ăn tại chỗ × 100% (cần nhập Tổng suất ăn trước)";
                _bang1Repository.Update(dongTyLe);
            }

            await _unitOfWork.SaveChangesAsync();
            return await ChiTietAsync(id);
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

            return await ChiTietAsync(id);
        }

        // ============================================================
        // TÍNH SỐ LƯỢT ĐÁNH GIÁ CBNV (MỨC 1-5) TỪ KETQUADANHGIA (HỆ KIOSK CŨ)
        // ============================================================
        //
        // Suy ra nhà thầu phụ trách 1 Nhà ăn (DiaDiemNhaAn) theo tháng qua
        // Phieu2_DanhGia.NhaAnId (tham chiếu logic tới DiaDiemNhaAn.ID — xác
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
        private async Task<decimal[]> TinhSoLuotCbnvTheoMucAsync(
            List<Phieu2DanhGia> danhSachPhieu2CuaNhaThau, int thang, int nam)
        {
            var soLuot = new decimal[6]; // [0] không dùng, [1..5]

            var nhaAnCuaNhaThau = danhSachPhieu2CuaNhaThau.Select(x => x.NhaAnId).Distinct().ToList();
            if (nhaAnCuaNhaThau.Count == 0) return soLuot;

            var phieu2TatCaTaiNhaAn = await _phieu2Repository.FindAsync(x =>
                nhaAnCuaNhaThau.Contains(x.NhaAnId) && x.Thang == thang && x.Nam == nam);

            var nhaAnRoRang = nhaAnCuaNhaThau
                .Where(nhaAnId => phieu2TatCaTaiNhaAn
                    .Where(p => p.NhaAnId == nhaAnId)
                    .Select(p => p.NhaThauId)
                    .Distinct()
                    .Count() == 1)
                .ToHashSet();
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
        // HELPERS
        // ============================================================

        private async Task<string> SinhSoHieuAsync(string maNhaThau, int nam)
        {
            // Phiếu 3 reset số hiệu theo NĂM (Thang = null) — xem mục 5.3 PhanTichNghiepVu.md
            var seq = await _soHieuService.SinhSoTiepTheoAsync("PHIEU3", maNhaThau, nam, null);
            return $"BC-{maNhaThau}-{nam}-{seq:D3}";
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
                    NguonDuLieu = mau.Ma == "TONG_SUAT_AN"
                        ? "Nhập tay — chưa có nguồn tự động"
                        : "Chưa tính",
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
    }
}
