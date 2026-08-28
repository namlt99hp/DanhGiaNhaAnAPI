using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Phieu4;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    // Xem quyết định thiết kế + các giả định nghiệp vụ đã xác nhận tại
    // 02. Phantich/modules/Phieu4_TongHopPhanBo.md.
    //
    // Bảng 1 (cấu trúc CỐ ĐỊNH — 4 nhóm dòng, giống mục 5.8 PhanTichNghiepVu.md):
    //   Nhóm 1 (1 dòng "Tổng số suất ăn"): NHẬP TAY hoàn toàn — không có nguồn tự
    //     động (giống TONG_SUAT_AN của Phiếu 3).
    //   Nhóm 2 (5 dòng "Số lượt đánh giá mức 1..5"): TỰ ĐỘNG (đổi nguồn
    //     2026-08-28) — đếm KetQuaDanhGia (hệ kiosk CBNV tự chấm 1-5) tại các
    //     Nhà ăn nhà thầu phụ trách trong [TuNgay, DenNgay], suy ra qua
    //     Phieu2_DanhGia.NhaAnId — xem TinhSoLuotCbnvTheoMucAsync. Trước đây
    //     dùng proxy Phieu2_KetQua.SoTieuChiDat (số tiêu chí "Đạt" trong
    //     checklist người đánh giá, không phải dữ liệu CBNV tự chấm thật).
    //   Nhóm 3 (1 dòng "Điểm đánh giá trung bình"): TỰ ĐỘNG =
    //     (1*sl1+2*sl2+3*sl3+4*sl4+5*sl5) / tổng lượt (nhóm 2), cùng cột nhà thầu.
    //   Nhóm 4 (1 dòng "Tỷ lệ CBNV tham gia đánh giá"): TỰ ĐỘNG = tổng nhóm 2 /
    //     nhóm 1 × 100%, chỉ tính được khi nhóm 1 (Tổng số suất ăn) đã nhập > 0.
    //
    // Quy tắc "tính lại": chỉ ghi đè các ô Bảng 1 (nhóm 2/3/4) có
    // ChinhSuaThuCong = false — giữ nguyên ô người dùng đã tự sửa. Nhóm 1 không
    // bao giờ bị tính lại (không có nguồn tự động).
    //
    // Bảng 2-5: CỐ ĐỊNH theo master NhomTieuChi/TieuChi (LoaiPhieu="PHIEU4",
    // NhomTieuChi.SoBang = 2..5, quản lý qua trang "Nhóm tiêu chí"/"Tiêu chí")
    // — không còn thêm/sửa/xóa dòng theo từng phiếu (đổi thiết kế 2026-08-27).
    // DongBoDongTuMauAsync tự đồng bộ dòng còn thiếu mỗi lần đọc phiếu
    // (ChiTietAsync) và lúc tạo phiếu mới (ThemAsync). Giá trị (Phieu4_GiaTri)
    // vẫn nhập tay hoàn toàn qua CapNhatGiaTriAsync — chỉ CẤU TRÚC DÒNG cố định.
    public class Phieu4Service : IPhieu4Service
    {
        private const int SO_BANG_TOI_DA = 5;

        private readonly IPhieu4TongHopRepository _phieuRepository;
        private readonly IPhieu4NhaThauRepository  _nhaThauCotRepository;
        private readonly IPhieu4BangRepository     _bangRepository;
        private readonly IPhieu4DongRepository     _dongRepository;
        private readonly IPhieu4GiaTriRepository   _giaTriRepository;
        private readonly IPhieu2DanhGiaRepository  _phieu2Repository;
        private readonly IKetQuaDanhGiaRepository  _ketQuaDanhGiaRepository;
        private readonly INhaThauRepository        _nhaThauRepository;
        private readonly INhomTieuChiRepository    _nhomTieuChiRepository;
        private readonly ITieuChiRepository        _tieuChiRepository;
        private readonly ISoHieuService            _soHieuService;
        private readonly IChuKyPhieuService         _chuKyPhieuService;
        private readonly INhatKyChinhSuaService     _nhatKyChinhSuaService;
        private readonly IUnitOfWork                _unitOfWork;

        public Phieu4Service(
            IPhieu4TongHopRepository phieuRepository,
            IPhieu4NhaThauRepository nhaThauCotRepository,
            IPhieu4BangRepository bangRepository,
            IPhieu4DongRepository dongRepository,
            IPhieu4GiaTriRepository giaTriRepository,
            IPhieu2DanhGiaRepository phieu2Repository,
            IKetQuaDanhGiaRepository ketQuaDanhGiaRepository,
            INhaThauRepository nhaThauRepository,
            INhomTieuChiRepository nhomTieuChiRepository,
            ITieuChiRepository tieuChiRepository,
            ISoHieuService soHieuService,
            IChuKyPhieuService chuKyPhieuService,
            INhatKyChinhSuaService nhatKyChinhSuaService,
            IUnitOfWork unitOfWork)
        {
            _phieuRepository        = phieuRepository;
            _nhaThauCotRepository   = nhaThauCotRepository;
            _bangRepository         = bangRepository;
            _dongRepository         = dongRepository;
            _giaTriRepository       = giaTriRepository;
            _phieu2Repository       = phieu2Repository;
            _ketQuaDanhGiaRepository = ketQuaDanhGiaRepository;
            _nhaThauRepository      = nhaThauRepository;
            _nhomTieuChiRepository  = nhomTieuChiRepository;
            _tieuChiRepository      = tieuChiRepository;
            _soHieuService          = soHieuService;
            _chuKyPhieuService      = chuKyPhieuService;
            _nhatKyChinhSuaService  = nhatKyChinhSuaService;
            _unitOfWork             = unitOfWork;
        }

        // ============================================================
        // DANH SÁCH
        // ============================================================

        public async Task<List<Phieu4TongHop>> DanhSachAsync(string? trangThai)
        {
            var query = _phieuRepository.Query();
            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.TrangThai == trangThai);

            return query.OrderByDescending(x => x.TuNgay).ThenByDescending(x => x.Id).ToList();
        }

        // ============================================================
        // CHI TIẾT
        // ============================================================

        public async Task<Phieu4ResponseDto> ChiTietAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);

            var nhaThau = (await _nhaThauCotRepository.FindAsync(x => x.PhieuId == id))
                          .OrderBy(x => x.ThuTu).ToList();

            var bang = (await _bangRepository.FindAsync(x => x.PhieuId == id))
                       .OrderBy(x => x.SoBang).ToList();

            // Bảng 2-5: cố định render theo nhóm/tiêu chí đang cấu hình ở master —
            // đồng bộ dòng còn thiếu (nếu admin vừa thêm nhóm/tiêu chí mới) mỗi lần
            // đọc phiếu, không cần thao tác "thêm dòng" thủ công (xem
            // 02. Phantich/modules/Phieu4_TongHopPhanBo.md).
            var nhaThauIdsHienTai = nhaThau.Select(x => x.NhaThauId).ToList();
            foreach (var b in bang.Where(x => x.SoBang != 1))
                await DongBoDongTuMauAsync(b, nhaThauIdsHienTai);

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
                    GiaTri = giaTri.Where(g => g.DongId == d.Id).ToList(),
                }).ToList(),
            }).ToList();

            return new Phieu4ResponseDto { Phieu = phieu, NhaThau = nhaThau, Bang = bangDto };
        }

        // ============================================================
        // TẠO MỚI
        // ============================================================

        public async Task<Phieu4ResponseDto> ThemAsync(Phieu4Request request, int? nguoiTaoId)
        {
            if (request.NhaThauIds == null || request.NhaThauIds.Count == 0)
                throw new ApiException("Vui lòng chọn ít nhất 1 nhà thầu");
            if (request.DenNgay < request.TuNgay)
                throw new ApiException("Ngày kết thúc phải sau ngày bắt đầu");

            var nhaThauHopLe = await _nhaThauRepository.FindAsync(x => request.NhaThauIds.Contains(x.Id));
            if (nhaThauHopLe.Count != request.NhaThauIds.Distinct().Count())
                throw new ApiException("Có nhà thầu không tồn tại trong danh sách đã chọn");

            var soHieu = await SinhSoHieuAsync(request.TuNgay.Year);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var phieu = new Phieu4TongHop
                {
                    SoHieu    = soHieu,
                    TuNgay    = request.TuNgay.Date,
                    DenNgay   = request.DenNgay.Date,
                    NguoiTao  = nguoiTaoId,
                    TrangThai = "NHAP",
                    NgayTao   = DateTime.Now,
                };
                await _phieuRepository.AddAsync(phieu);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật

                var thuTu = 0;
                foreach (var nhaThauId in request.NhaThauIds.Distinct())
                {
                    await _nhaThauCotRepository.AddAsync(new Phieu4NhaThau
                    {
                        PhieuId = phieu.Id,
                        NhaThauId = nhaThauId,
                        ThuTu = thuTu++,
                    });
                }
                await _unitOfWork.SaveChangesAsync();

                var nhaThauIds = request.NhaThauIds.Distinct().ToList();
                await KhoiTaoBang1Async(phieu.Id, nhaThauIds);
                for (var soBang = 2; soBang <= SO_BANG_TOI_DA; soBang++)
                {
                    var bang = new Phieu4Bang
                    {
                        PhieuId = phieu.Id,
                        SoBang = soBang,
                        TenBang = $"Bảng {soBang}",
                    };
                    await _bangRepository.AddAsync(bang);
                    await _unitOfWork.SaveChangesAsync(); // cần Id thật để seed dòng

                    await DongBoDongTuMauAsync(bang, nhaThauIds);
                }

                await transaction.CommitAsync();

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
        // XÓA
        // ============================================================

        public async Task XoaAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP")
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
            _nhaThauCotRepository.RemoveRange(nhaThau);

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
        // TÍNH LẠI BẢNG 1 TỰ ĐỘNG (từ Phieu2_DanhGia)
        // ============================================================

        public async Task<Phieu4ResponseDto> TinhLaiAsync(int id)
        {
            var phieu = await _phieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu tổng hợp", StatusCodes.Status404NotFound);

            var danhSachNhaThau = await _nhaThauCotRepository.FindAsync(x => x.PhieuId == id);
            var bang1 = (await _bangRepository.FindAsync(x => x.PhieuId == id && x.SoBang == 1))
                        .FirstOrDefault();
            if (bang1 == null) return await ChiTietAsync(id);

            var dongBang1 = await _dongRepository.FindAsync(x => x.BangId == bang1.Id);
            var dongNhom1 = dongBang1.FirstOrDefault(x => x.NhomSo == 1);
            var dongNhom2 = dongBang1.Where(x => x.NhomSo == 2).OrderBy(x => x.Stt).ToList();
            var dongNhom3 = dongBang1.FirstOrDefault(x => x.NhomSo == 3);
            var dongNhom4 = dongBang1.FirstOrDefault(x => x.NhomSo == 4);

            var dongIdsBang1 = dongBang1.Select(x => x.Id).ToList();
            var giaTriBang1 = await _giaTriRepository.FindAsync(x => dongIdsBang1.Contains(x.DongId));

            foreach (var cot in danhSachNhaThau)
            {
                var danhSachPhieu2 = (await _phieu2Repository.FindAsync(x => x.NhaThauId == cot.NhaThauId))
                    .Where(x => x.ThoiGianTu.HasValue
                             && x.ThoiGianTu.Value.Date >= phieu.TuNgay.Date
                             && x.ThoiGianTu.Value.Date <= phieu.DenNgay.Date)
                    .ToList();

                var soLuotTheoMuc = await TinhSoLuotCbnvTheoMucAsync(danhSachPhieu2, phieu.TuNgay, phieu.DenNgay);
                var tongLuot = soLuotTheoMuc[1] + soLuotTheoMuc[2] + soLuotTheoMuc[3] + soLuotTheoMuc[4] + soLuotTheoMuc[5];

                // Nhóm 2: 5 dòng, mỗi dòng ứng với 1 mức điểm (Stt = mức)
                foreach (var d in dongNhom2)
                {
                    var muc = d.Stt ?? 0;
                    if (muc is < 1 or > 5) continue;
                    await GhiGiaTriTuDongAsync(giaTriBang1, d.Id, cot.NhaThauId, soLuotTheoMuc[muc]);
                }

                // Nhóm 1 (nhập tay) — lấy giá trị hiện tại để tính nhóm 3/4
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

            await _unitOfWork.SaveChangesAsync();
            return await ChiTietAsync(id);
        }

        // ============================================================
        // TÍNH SỐ LƯỢT ĐÁNH GIÁ CBNV (MỨC 1-5) TỪ KETQUADANHGIA (HỆ KIOSK CŨ)
        // ============================================================
        //
        // Suy ra nhà thầu phụ trách 1 Nhà ăn (DiaDiemNhaAn) theo khoảng ngày
        // qua Phieu2_DanhGia.NhaAnId (tham chiếu logic tới DiaDiemNhaAn.ID —
        // xác nhận nghiệp vụ 2026-08-27, xem
        // modules/Phieu2_DanhGiaSuatAn.md). Thay cho proxy cũ (đếm
        // Phieu2_KetQua.SoTieuChiDat — số tiêu chí "Đạt" trong checklist
        // người đánh giá, không phải dữ liệu CBNV tự chấm) — xác nhận nghiệp
        // vụ 2026-08-28, cùng công thức với Phieu3Service.
        //
        // Quy tắc quy Nhà ăn về nhà thầu: Nhà ăn nào có Phiếu 2 của NHIỀU nhà
        // thầu khác nhau trong cùng khoảng ngày thì bị LOẠI khỏi tính tự
        // động cho TẤT CẢ nhà thầu (không đủ căn cứ quy về 1 bên). Nhà ăn
        // không có Phiếu 2 nào trong khoảng ngày cũng không tính được.
        private async Task<decimal[]> TinhSoLuotCbnvTheoMucAsync(
            List<Phieu2DanhGia> danhSachPhieu2CuaNhaThau, DateTime tuNgay, DateTime denNgay)
        {
            var soLuot = new decimal[6]; // [0] không dùng, [1..5]

            var nhaAnCuaNhaThau = danhSachPhieu2CuaNhaThau.Select(x => x.NhaAnId).Distinct().ToList();
            if (nhaAnCuaNhaThau.Count == 0) return soLuot;

            var phieu2TatCaTaiNhaAn = await _phieu2Repository.FindAsync(x =>
                nhaAnCuaNhaThau.Contains(x.NhaAnId) &&
                x.ThoiGianTu.HasValue &&
                x.ThoiGianTu.Value.Date >= tuNgay.Date &&
                x.ThoiGianTu.Value.Date <= denNgay.Date);

            var nhaAnRoRang = nhaAnCuaNhaThau
                .Where(nhaAnId => phieu2TatCaTaiNhaAn
                    .Where(p => p.NhaAnId == nhaAnId)
                    .Select(p => p.NhaThauId)
                    .Distinct()
                    .Count() == 1)
                .ToHashSet();
            if (nhaAnRoRang.Count == 0) return soLuot;

            var ketQuaCbnv = await _ketQuaDanhGiaRepository.FindAsync(x =>
                nhaAnRoRang.Contains(x.DiaDiem_ID) &&
                x.ThoiGianDanhGia.Date >= tuNgay.Date &&
                x.ThoiGianDanhGia.Date <= denNgay.Date);

            foreach (var kq in ketQuaCbnv)
            {
                if (kq.DiemDanhGia is >= 1 and <= 5) soLuot[kq.DiemDanhGia]++;
            }
            return soLuot;
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
            var dongHopLeIds = (await _dongRepository.FindAsync(x => bangIds.Contains(x.BangId))).Select(x => x.Id).ToHashSet();

            foreach (var item in request.GiaTri)
            {
                if (!dongHopLeIds.Contains(item.DongId))
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

            await _unitOfWork.SaveChangesAsync();
            return await ChiTietAsync(id);
        }

        // ============================================================
        // SỬA TÊN BẢNG
        // ============================================================
        // Bảng 2-5 KHÔNG còn cho thêm/sửa/xóa dòng thủ công — nội dung dòng
        // cố định theo NhomTieuChi/TieuChi (master), tự đồng bộ mỗi lần đọc
        // phiếu qua DongBoDongTuMauAsync (xem ChiTietAsync). Muốn đổi nội dung
        // thì sửa ở master (trang "Nhóm tiêu chí"/"Tiêu chí"), không sửa trực
        // tiếp trên từng phiếu.

        public async Task<Phieu4ResponseDto> SuaBangAsync(int id, int bangId, Phieu4BangRequest request)
        {
            var bang = await LayBangHopLeAsync(id, bangId, choPhepBang1: true);
            bang.TenBang = request.TenBang;
            _bangRepository.Update(bang);
            await _unitOfWork.SaveChangesAsync();

            return await ChiTietAsync(id);
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
            var bang1 = new Phieu4Bang { PhieuId = phieuId, SoBang = 1, TenBang = "Tổng hợp đánh giá theo nhà thầu" };
            await _bangRepository.AddAsync(bang1);
            await _unitOfWork.SaveChangesAsync();

            var dongMoi = new List<Phieu4Dong>
            {
                new() { BangId = bang1.Id, NhomSo = 1, Stt = 1, NoiDung = "Tổng số suất ăn", Dvt = "Suất", LoaiDong = "NHAP_TAY" },
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

        // Đồng bộ dòng cho 1 bảng (2-5) từ master data (NhomTieuChi/TieuChi có
        // LoaiPhieu = "PHIEU4", SoBang = bảng tương ứng) — Bảng 2-5 CỐ ĐỊNH
        // theo nhóm/tiêu chí cấu hình ở master, không còn thêm/sửa/xóa dòng
        // thủ công theo từng phiếu. Idempotent: chỉ thêm dòng cho TieuChiId
        // CHƯA có trong bảng (so theo Phieu4_Dong.TieuChiId), không tạo trùng
        // — an toàn khi gọi lại nhiều lần (lúc tạo phiếu mới lẫn mỗi lần
        // ChiTietAsync, xem 02. Phantich/modules/Phieu4_TongHopPhanBo.md).
        // Nếu chưa cấu hình master cho bảng này thì không tạo dòng nào.
        private async Task DongBoDongTuMauAsync(Phieu4Bang bang, List<int> nhaThauIds)
        {
            var nhomMau = (await _nhomTieuChiRepository.FindAsync(
                    x => x.LoaiPhieu == "PHIEU4" && x.SoBang == bang.SoBang && x.DangHoatDong))
                .OrderBy(x => x.ThuTu).ToList();
            if (nhomMau.Count == 0) return;

            var nhomIds = nhomMau.Select(x => x.Id).ToList();
            var tieuChiMau = (await _tieuChiRepository.FindAsync(
                    x => x.NhomId.HasValue && nhomIds.Contains(x.NhomId.Value) && x.DangHoatDong))
                .ToList();
            if (tieuChiMau.Count == 0) return;

            var dongHienTai = await _dongRepository.FindAsync(x => x.BangId == bang.Id);
            var tieuChiIdDaCo = dongHienTai
                .Where(x => x.TieuChiId.HasValue)
                .Select(x => x.TieuChiId!.Value)
                .ToHashSet();
            var stt = dongHienTai.Count == 0 ? 0 : dongHienTai.Max(x => x.Stt ?? 0);

            var dongMoi = new List<Phieu4Dong>();
            foreach (var nhom in nhomMau)
            {
                var tieuChiCuaNhom = tieuChiMau.Where(x => x.NhomId == nhom.Id).OrderBy(x => x.ThuTu);
                foreach (var tc in tieuChiCuaNhom)
                {
                    if (tieuChiIdDaCo.Contains(tc.Id)) continue; // đã đồng bộ rồi — không tạo trùng

                    dongMoi.Add(new Phieu4Dong
                    {
                        BangId = bang.Id,
                        Stt = ++stt,
                        NoiDung = tc.NoiDung,
                        Dvt = tc.Dvt,
                        LoaiDong = string.IsNullOrWhiteSpace(tc.LoaiDong) ? "NHAP_TAY" : tc.LoaiDong,
                        CongThuc = tc.CongThuc,
                        TieuChiId = tc.Id,
                        NhomTieuChiId = nhom.Id,
                    });
                }
            }
            if (dongMoi.Count == 0) return;

            await _dongRepository.AddRangeAsync(dongMoi);
            await _unitOfWork.SaveChangesAsync();

            var giaTriMoi = new List<Phieu4GiaTri>();
            foreach (var dong in dongMoi)
                foreach (var nhaThauId in nhaThauIds)
                    giaTriMoi.Add(new Phieu4GiaTri { DongId = dong.Id, NhaThauId = nhaThauId });

            await _giaTriRepository.AddRangeAsync(giaTriMoi);
            await _unitOfWork.SaveChangesAsync();
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
