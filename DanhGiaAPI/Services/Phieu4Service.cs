using DanhGiaAPI.Common;
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
    // Bảng 1 (cấu trúc CỐ ĐỊNH — 4 nhóm dòng, giống mục 5.8 PhanTichNghiepVu.md):
    //   Nhóm 1 (1 dòng "Tổng số suất ăn"): TỰ ĐỘNG (đổi nguồn từ nhập tay,
    //     giống TONG_SUAT_AN của Phiếu 3) — tổng DuLieuCom.Com_ThucTe_ALL
    //     trong [TuNgay, DenNgay], tại các Nhà ăn nhà thầu phụ trách, suy ra
    //     qua Phieu2_NhaAn (cùng tập "nhà ăn rõ ràng" dùng cho Nhóm 2, xem
    //     LayDiaDiemRoRangCuaNhaThauAsync + TinhTongSuatAnAsync).
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
    // Quy tắc "tính lại": chỉ ghi đè các ô Bảng 1 (nhóm 1/2/3/4) có
    // ChinhSuaThuCong = false — giữ nguyên ô người dùng đã tự sửa.
    //
    // Bảng 2: CỐ ĐỊNH hard-code (đổi từ master sang hard-code 2026-09-01, xem
    // TieuChiBang2/KhoiTaoBang2CoDinhAsync/TinhLaiBang2Async) — 12 dòng (2
    // phòng ban P.ĐN/P.ATMT × 6 tiêu chí), P.ĐN đa số TỰ ĐỘNG từ Phiếu 2,
    // P.ATMT chỉ VSATTP TỰ ĐỘNG từ Phiếu 1, "Đa dạng thực đơn" luôn nhập tay.
    //
    // Bảng 3-5: đổi từ master NhomTieuChi/TieuChi sang CỐ ĐỊNH hard-code
    // (2026-09-02, xem 02. Phantich/modules/Phieu4_TongHopPhanBo.md) — master
    // chưa từng được cấu hình cho 3 bảng này nên trước đây luôn rỗng. Nội dung
    // lấy đúng theo mẫu giấy BM.09/HD.22.04:
    //   Bảng 3.1 ("Cơ chế hiệu chỉnh"): TĨNH hoàn toàn, không có ở DB — render
    //     thẳng ở FE (Phieu4FormPage.tsx), không có cột nhà thầu.
    //   Bảng 3.2 ("Điểm tổng hợp của các Nhà thầu"): 3 dòng CỐ ĐỊNH (hệ số
    //     hiệu chỉnh / điểm TB CBNV sau hiệu chỉnh / điểm tổng hợp theo trọng
    //     số), NHẬP TAY hoàn toàn, dùng ĐÚNG cơ chế Phieu4_GiaTri theo cột nhà
    //     thầu như Bảng 1/2 — xem DongBoBang3CoDinhAsync. FE hiển thị XOAY
    //     TRỤC (nhà thầu là hàng) để khớp bản giấy, nhưng lưu trữ vẫn dòng ×
    //     cột nhà thầu như mọi bảng khác.
    //   Bảng 4 ("Phân bổ theo Nhà ăn/Điểm ăn"): 1 dòng / dbo.DiaDiemNhaAn đang
    //     active, đồng bộ tự động mỗi lần đọc phiếu — xem
    //     DongBoBang4TuDiaDiemAsync. KHÔNG có cột nhà thầu — giá trị TỰ ĐỘNG
    //     (đổi từ nhập tay) lưu ở Phieu4_Dong.GiaTriChung (không qua
    //     Phieu4_GiaTri) = tổng DuLieuCom.Com_ThucTe_ALL trong [TuNgay, DenNgay]
    //     tại đúng địa điểm đó, xem TinhLaiGiaTriChungTheoDiaDiemAsync.
    //   Bảng 5 ("Phân bổ theo Nhà thầu"): 1 dòng / cặp (NhaThau, DiaDiemNhaAn)
    //     suy ra từ Phiếu 2 trong [TuNgay, DenNgay] của phiếu (CHỈ Phiếu 2 —
    //     Phiếu 1 không có liên kết nào tới DiaDiemNhaAn), đồng bộ tự động —
    //     xem DongBoBang5TuPhieu2Async + LayDiaDiemRoRangCuaNhaThauAsync. Giá
    //     trị cũng lưu ở GiaTriChung (không chia theo cột nhà thầu, đã cố định
    //     ở CẤP DÒNG qua Phieu4_Dong.NhaThauId), TỰ ĐỘNG cùng công thức Bảng 4
    //     (TinhLaiGiaTriChungTheoDiaDiemAsync — 1 dòng ứng đúng 1 địa điểm nên
    //     không cần lọc thêm theo nhà thầu).
    // Cả 2 bảng 4/5 chỉ THÊM dòng còn thiếu mỗi lần đọc phiếu (idempotent),
    // không bao giờ xóa dòng cũ — giữ lịch sử nếu 1 địa điểm/nhà thầu không
    // còn "rõ ràng" ở lần tính sau. Giá trị TỰ ĐỘNG chỉ ghi đè ô chưa bị sửa
    // tay (Phieu4_Dong.ChinhSuaThuCong = false), tính lại mỗi lần đọc phiếu.
    public class Phieu4Service : IPhieu4Service
    {
        private readonly IPhieu4TongHopRepository _phieuRepository;
        private readonly IPhieu4NhaThauRepository  _nhaThauCotRepository;
        private readonly IPhieu4BangRepository     _bangRepository;
        private readonly IPhieu4DongRepository     _dongRepository;
        private readonly IPhieu4GiaTriRepository   _giaTriRepository;
        private readonly IPhieu2DanhGiaRepository  _phieu2Repository;
        private readonly IPhieu2TieuChiRepository  _phieu2TieuChiRepository;
        private readonly IPhieu2NhaAnRepository    _phieu2NhaAnRepository;
        private readonly IPhieu1KiemTraRepository  _phieu1Repository;
        private readonly IPhieu1KetLuanRepository  _phieu1KetLuanRepository;
        private readonly IKetQuaDanhGiaRepository  _ketQuaDanhGiaRepository;
        private readonly IDuLieuComRepository      _duLieuComRepository;
        private readonly INhaThauRepository        _nhaThauRepository;
        private readonly IPhongBanRepository       _phongBanRepository;
        private readonly IDiaDiemNhaAnRepository   _diaDiemNhaAnRepository;
        private readonly ISoHieuService            _soHieuService;
        private readonly IChuKyPhieuService         _chuKyPhieuService;
        private readonly INhatKyChinhSuaService     _nhatKyChinhSuaService;
        private readonly IUnitOfWork                _unitOfWork;

        // Bảng 2 — CỐ ĐỊNH (đổi từ master NhomTieuChi/TieuChi sang hard-code,
        // xác nhận nghiệp vụ 2026-09-01, giống Phieu3Service): 6 tiêu chí ×
        // 2 phòng ban (P.ĐN Stt 1-6, P.ATMT Stt 7-12 — dùng Stt liên tục toàn
        // bảng để 12 dòng không đụng nhau khi FE sort theo Stt). TC3 "Đa dạng
        // thực đơn" không có nguồn tự động ở nhánh nào -> luôn nhập tay.
        private static readonly (int Stt, string Ten, string? MaTieuChiPhieu2)[] TieuChiBang2 = new[]
        {
            (1, "Tuân thủ đúng quy định về vệ sinh an toàn thực phẩm", "VSATTP"),
            (2, "Tuân thủ định lượng theo thực đơn đã được phê duyệt", "DINH_LUONG_THUC_DON"),
            (3, "Đa dạng thực đơn", (string?)null),
            (4, "Tuân thủ hợp đồng, bản cam kết, quy trình báo cáo", "DIEU_KHOAN_KHAC"),
            (5, "Thái độ phối hợp, cầu thị cải tiến", "THAI_DO_PHOI_HOP"),
            (6, "Phản hồi sự cố, xử lý khiếu nại nhanh chóng", "PHAN_HOI_SU_CO"),
        };

        // Bảng 3.2 — 3 dòng CỐ ĐỊNH, NHẬP TAY hoàn toàn (chưa có công thức tự
        // động cho hệ số hiệu chỉnh/điểm sau hiệu chỉnh — xác nhận nghiệp vụ
        // 2026-09-02). FE hiển thị xoay trục (nhà thầu là hàng, xem
        // Phieu4FormPage.tsx) nhưng dòng/giá trị vẫn lưu dòng × cột nhà thầu
        // như các bảng khác.
        private static readonly (int Stt, string Ten)[] TieuChiBang3 = new[]
        {
            (1, "Hệ số hiệu chỉnh cho điểm đánh giá CBNV"),
            (2, "Điểm đánh giá trung bình của CBNV sau hiệu chỉnh"),
            (3, "Điểm tổng hợp theo trọng số đánh giá từ CBNV và P.CHN sau hiệu chỉnh"),
        };

        public Phieu4Service(
            IPhieu4TongHopRepository phieuRepository,
            IPhieu4NhaThauRepository nhaThauCotRepository,
            IPhieu4BangRepository bangRepository,
            IPhieu4DongRepository dongRepository,
            IPhieu4GiaTriRepository giaTriRepository,
            IPhieu2DanhGiaRepository phieu2Repository,
            IPhieu2TieuChiRepository phieu2TieuChiRepository,
            IPhieu2NhaAnRepository phieu2NhaAnRepository,
            IPhieu1KiemTraRepository phieu1Repository,
            IPhieu1KetLuanRepository phieu1KetLuanRepository,
            IKetQuaDanhGiaRepository ketQuaDanhGiaRepository,
            IDuLieuComRepository duLieuComRepository,
            INhaThauRepository nhaThauRepository,
            IPhongBanRepository phongBanRepository,
            IDiaDiemNhaAnRepository diaDiemNhaAnRepository,
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
            _phieu2TieuChiRepository = phieu2TieuChiRepository;
            _phieu2NhaAnRepository  = phieu2NhaAnRepository;
            _phieu1Repository       = phieu1Repository;
            _phieu1KetLuanRepository = phieu1KetLuanRepository;
            _ketQuaDanhGiaRepository = ketQuaDanhGiaRepository;
            _duLieuComRepository    = duLieuComRepository;
            _nhaThauRepository      = nhaThauRepository;
            _phongBanRepository     = phongBanRepository;
            _diaDiemNhaAnRepository = diaDiemNhaAnRepository;
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

            // Bảng 2-5: CỐ ĐỊNH (đổi từ master NhomTieuChi/TieuChi 2026-09-02,
            // xem doc comment đầu file) — đồng bộ dòng còn thiếu mỗi lần đọc
            // phiếu, không cần thao tác "thêm dòng" thủ công.
            var nhaThauIdsHienTai = nhaThau.Select(x => x.NhaThauId).ToList();
            var bang2Entity = bang.FirstOrDefault(x => x.SoBang == 2);
            if (bang2Entity != null)
                await DongBoBang2CoDinhAsync(bang2Entity, nhaThauIdsHienTai);

            var bang3Entity = bang.FirstOrDefault(x => x.SoBang == 3);
            if (bang3Entity != null)
                await DongBoBang3CoDinhAsync(bang3Entity, nhaThauIdsHienTai);

            var bang4Entity = bang.FirstOrDefault(x => x.SoBang == 4);
            if (bang4Entity != null)
            {
                await DongBoBang4TuDiaDiemAsync(bang4Entity);
                await TinhLaiGiaTriChungTheoDiaDiemAsync(bang4Entity.Id, phieu.TuNgay, phieu.DenNgay);
            }

            var bang5Entity = bang.FirstOrDefault(x => x.SoBang == 5);
            if (bang5Entity != null)
            {
                await DongBoBang5TuPhieu2Async(bang5Entity, phieu, nhaThau);
                await TinhLaiGiaTriChungTheoDiaDiemAsync(bang5Entity.Id, phieu.TuNgay, phieu.DenNgay);
            }

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
                var nhaThauCotMoi = new List<Phieu4NhaThau>();
                foreach (var nhaThauId in request.NhaThauIds.Distinct())
                {
                    var cot = new Phieu4NhaThau
                    {
                        PhieuId = phieu.Id,
                        NhaThauId = nhaThauId,
                        ThuTu = thuTu++,
                    };
                    await _nhaThauCotRepository.AddAsync(cot);
                    nhaThauCotMoi.Add(cot);
                }
                await _unitOfWork.SaveChangesAsync();

                var nhaThauIds = request.NhaThauIds.Distinct().ToList();
                await KhoiTaoBang1Async(phieu.Id, nhaThauIds);

                var bang2 = new Phieu4Bang { PhieuId = phieu.Id, SoBang = 2, TenBang = "2. Tổng hợp kết quả đánh giá từ phòng chức năng" };
                await _bangRepository.AddAsync(bang2);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật để seed dòng
                await DongBoBang2CoDinhAsync(bang2, nhaThauIds);

                var bang3 = new Phieu4Bang { PhieuId = phieu.Id, SoBang = 3, TenBang = "3. Điểm tổng hợp của mỗi Nhà thầu" };
                await _bangRepository.AddAsync(bang3);
                await _unitOfWork.SaveChangesAsync();
                await DongBoBang3CoDinhAsync(bang3, nhaThauIds);

                var bang4 = new Phieu4Bang { PhieuId = phieu.Id, SoBang = 4, TenBang = "4. Phân bổ số lượng suất ăn theo Nhà ăn/Điểm ăn phục vụ" };
                await _bangRepository.AddAsync(bang4);
                await _unitOfWork.SaveChangesAsync();
                await DongBoBang4TuDiaDiemAsync(bang4);

                var bang5 = new Phieu4Bang { PhieuId = phieu.Id, SoBang = 5, TenBang = "5. Phân bổ số lượng suất ăn theo Nhà thầu" };
                await _bangRepository.AddAsync(bang5);
                await _unitOfWork.SaveChangesAsync();
                await DongBoBang5TuPhieu2Async(bang5, phieu, nhaThauCotMoi);

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

            // Bảng 5: dòng gắn CỨNG với nhà thầu này (Phieu4Dong.NhaThauId, khác
            // nghĩa Phieu4GiaTri.NhaThauId — xem comment ở Phieu4Dong.cs) — xóa
            // luôn để không còn nhóm "ma" của 1 nhà thầu đã bị bỏ khỏi phiếu.
            var dongBang5CuaNhaThau = dong.Where(x => x.NhaThauId == nhaThauId).ToList();
            if (dongBang5CuaNhaThau.Count > 0)
                _dongRepository.RemoveRange(dongBang5CuaNhaThau);

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
                var nhaAnRoRang = await LayDiaDiemRoRangCuaNhaThauAsync(cot.NhaThauId, phieu.TuNgay, phieu.DenNgay);
                var soLuotTheoMuc = await TinhSoLuotCbnvTheoMucAsync(nhaAnRoRang, phieu.TuNgay, phieu.DenNgay);
                var tongLuot = soLuotTheoMuc[1] + soLuotTheoMuc[2] + soLuotTheoMuc[3] + soLuotTheoMuc[4] + soLuotTheoMuc[5];

                // Nhóm 2: 5 dòng, mỗi dòng ứng với 1 mức điểm (Stt = mức)
                foreach (var d in dongNhom2)
                {
                    var muc = d.Stt ?? 0;
                    if (muc is < 1 or > 5) continue;
                    await GhiGiaTriTuDongAsync(giaTriBang1, d.Id, cot.NhaThauId, soLuotTheoMuc[muc]);
                }

                // Nhóm 1: Tổng số suất ăn — tự động = tổng DuLieuCom.Com_ThucTe_ALL
                // tại các Nhà ăn nhà thầu phụ trách (cùng nhaAnRoRang ở trên).
                if (dongNhom1 != null)
                {
                    var tongSuatAn = await TinhTongSuatAnAsync(nhaAnRoRang, phieu.TuNgay, phieu.DenNgay);
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
            return await ChiTietAsync(id);
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
                // ---- P.ĐN (NhomSo = 1) ----
                var phieu2CuaNhaThau = (await _phieu2Repository.FindAsync(x => x.NhaThauId == cot.NhaThauId))
                    .Where(x => x.ThoiGianTu.HasValue
                             && x.ThoiGianTu.Value.Date >= phieu.TuNgay.Date
                             && x.ThoiGianTu.Value.Date <= phieu.DenNgay.Date)
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

                    var cacDiem = tieuChiPhieu2
                        .Where(x => x.MaTieuChi == tc.MaTieuChiPhieu2 && x.Diem.HasValue)
                        .Select(x => x.Diem!.Value)
                        .ToList();
                    decimal? giaTri = cacDiem.Count > 0 ? Math.Round(cacDiem.Average(), 2) : null;
                    await GhiGiaTriTuDongAsync(giaTriBang2, dong.Id, cot.NhaThauId, giaTri);
                }

                // ---- P.ATMT (NhomSo = 2) — chỉ VSATTP (Stt = 7) ----
                if (pbAtmt != null)
                {
                    var dongVsattpAtmt = dongBang2.FirstOrDefault(x => x.NhomSo == 2 && x.Stt == 7);
                    if (dongVsattpAtmt != null)
                    {
                        var phieu1CuaKhoangNgay = await _phieu1Repository.FindAsync(x =>
                            x.NhaThauId == cot.NhaThauId && x.PhongBanId == pbAtmt.Id &&
                            x.NgayKiemTra >= phieu.TuNgay.Date && x.NgayKiemTra <= phieu.DenNgay.Date);
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
        // TÍNH SỐ LƯỢT ĐÁNH GIÁ CBNV (MỨC 1-5) TỪ KETQUADANHGIA (HỆ KIOSK CŨ)
        // ============================================================
        //
        // Suy ra nhà thầu phụ trách 1 Nhà ăn (DiaDiemNhaAn) theo khoảng ngày
        // qua Phieu2_NhaAn (tham chiếu logic tới DiaDiemNhaAn.ID —
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
        //
        // Tách phần suy luận "nhà ăn rõ ràng" ra helper riêng
        // (LayDiaDiemRoRangCuaNhaThauAsync) để dùng lại cho Bảng 5
        // (DongBoBang5TuPhieu2Async) — xem 02. Phantich/modules/Phieu4_TongHopPhanBo.md.
        private async Task<decimal[]> TinhSoLuotCbnvTheoMucAsync(HashSet<int> nhaAnRoRang, DateTime tuNgay, DateTime denNgay)
        {
            var soLuot = new decimal[6]; // [0] không dùng, [1..5]
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

        // Tổng DuLieuCom.Com_ThucTe_ALL trong [TuNgay, DenNgay], tại các Nhà ăn
        // "rõ ràng" (ID_DiemAn = DiaDiemNhaAn.ID) mà nhà thầu phụ trách — cùng
        // tập nhaAnRoRang dùng cho TinhSoLuotCbnvTheoMucAsync.
        private async Task<int> TinhTongSuatAnAsync(HashSet<int> nhaAnRoRang, DateTime tuNgay, DateTime denNgay)
        {
            if (nhaAnRoRang.Count == 0) return 0;

            var duLieuCom = await _duLieuComRepository.FindAsync(x =>
                nhaAnRoRang.Contains(x.ID_DiemAn) &&
                x.Ngay >= tuNgay.Date && x.Ngay <= denNgay.Date);

            return duLieuCom.Sum(x => x.Com_ThucTe_ALL ?? 0);
        }

        // ============================================================
        // TÍNH LẠI BẢNG 4/5 TỰ ĐỘNG TỪ DULIEUCOM (theo Nhà ăn/Điểm ăn)
        // ============================================================
        //
        // GiaTriChung tại mỗi dòng (ứng với 1 DiaDiemNhaAnId) = tổng
        // DuLieuCom.Com_ThucTe_ALL trong [TuNgay, DenNgay] của phiếu, tại đúng
        // ID_DiemAn = DiaDiemNhaAnId — dùng chung cho cả Bảng 4 (không phân
        // biệt nhà thầu) lẫn Bảng 5 (1 dòng đã ứng với đúng 1 cặp nhà
        // thầu/địa điểm "rõ ràng" — xem DongBoBang5TuPhieu2Async — nên công
        // thức theo địa điểm là như nhau, không cần lọc thêm theo nhà thầu).
        // Chỉ ghi đè ô chưa bị sửa tay (ChinhSuaThuCong = false), gọi mỗi lần
        // đọc phiếu (ChiTietAsync) ngay sau khi đồng bộ dòng còn thiếu.
        private async Task TinhLaiGiaTriChungTheoDiaDiemAsync(int bangId, DateTime tuNgay, DateTime denNgay)
        {
            var dongCanTinh = (await _dongRepository.FindAsync(x => x.BangId == bangId && x.DiaDiemNhaAnId.HasValue))
                .Where(x => !x.ChinhSuaThuCong)
                .ToList();
            if (dongCanTinh.Count == 0) return;

            var diaDiemIds = dongCanTinh.Select(x => x.DiaDiemNhaAnId!.Value).Distinct().ToHashSet();
            var duLieuCom = await _duLieuComRepository.FindAsync(x =>
                diaDiemIds.Contains(x.ID_DiemAn) &&
                x.Ngay >= tuNgay.Date && x.Ngay <= denNgay.Date);

            var tongTheoDiaDiem = duLieuCom
                .GroupBy(x => x.ID_DiemAn)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Com_ThucTe_ALL ?? 0));

            var coThayDoi = false;
            foreach (var dong in dongCanTinh)
            {
                var tong = tongTheoDiaDiem.TryGetValue(dong.DiaDiemNhaAnId!.Value, out var t) ? t : 0;
                if (dong.GiaTriChung == tong) continue;
                dong.GiaTriChung = tong;
                _dongRepository.Update(dong);
                coThayDoi = true;
            }
            if (coThayDoi) await _unitOfWork.SaveChangesAsync();
        }

        // Suy ra tập DiaDiemNhaAnId "rõ ràng" (không dùng chung bởi nhiều nhà
        // thầu khác nhau) mà 1 nhà thầu phụ trách trong khoảng ngày, qua
        // Phieu2_NhaAn — xem quy tắc loại trừ ở comment trên.
        private async Task<HashSet<int>> LayDiaDiemRoRangCuaNhaThauAsync(int nhaThauId, DateTime tuNgay, DateTime denNgay)
        {
            var phieu2CuaNhaThau = (await _phieu2Repository.FindAsync(x => x.NhaThauId == nhaThauId))
                .Where(x => x.ThoiGianTu.HasValue
                         && x.ThoiGianTu.Value.Date >= tuNgay.Date
                         && x.ThoiGianTu.Value.Date <= denNgay.Date)
                .ToList();

            // 1 Phiếu 2 có thể gộp NHIỀU nhà ăn (Phieu2_NhaAn) — suy ra tập
            // Nhà ăn của nhà thầu qua tất cả nhà ăn nằm trong các Phiếu 2 của
            // họ trong khoảng ngày.
            var phieu2CuaNhaThauIds = phieu2CuaNhaThau.Select(x => x.Id).ToList();
            var lienKetCuaNhaThau = phieu2CuaNhaThauIds.Count > 0
                ? await _phieu2NhaAnRepository.FindAsync(x => phieu2CuaNhaThauIds.Contains(x.PhieuId))
                : new List<Phieu2NhaAn>();
            var nhaAnCuaNhaThau = lienKetCuaNhaThau.Select(x => x.NhaAnId).Distinct().ToList();
            if (nhaAnCuaNhaThau.Count == 0) return new HashSet<int>();

            // Tất cả Phiếu 2 (MỌI nhà thầu) trong khoảng ngày, để xét "nhà ăn rõ ràng"
            var phieu2TrongKhoang = (await _phieu2Repository.FindAsync(x =>
                    x.ThoiGianTu.HasValue &&
                    x.ThoiGianTu.Value.Date >= tuNgay.Date &&
                    x.ThoiGianTu.Value.Date <= denNgay.Date))
                .ToList();
            var phieu2TrongKhoangIds = phieu2TrongKhoang.Select(x => x.Id).ToList();
            var nhaThauCuaPhieu = phieu2TrongKhoang.ToDictionary(x => x.Id, x => x.NhaThauId);

            var lienKetTrongKhoang = phieu2TrongKhoangIds.Count > 0
                ? await _phieu2NhaAnRepository.FindAsync(x =>
                    phieu2TrongKhoangIds.Contains(x.PhieuId) && nhaAnCuaNhaThau.Contains(x.NhaAnId))
                : new List<Phieu2NhaAn>();

            return nhaAnCuaNhaThau
                .Where(nhaAnId => lienKetTrongKhoang
                    .Where(l => l.NhaAnId == nhaAnId)
                    .Select(l => nhaThauCuaPhieu[l.PhieuId])
                    .Distinct()
                    .Count() == 1)
                .ToHashSet();
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
            return await ChiTietAsync(id);
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
        // lại nhiều lần (lúc tạo phiếu mới lẫn mỗi lần ChiTietAsync) — cùng
        // kiểu với DongBoBang3CoDinhAsync (Bảng 3). Giúp phiếu tạo TRƯỚC đợt
        // đổi Bảng 2 sang cố định (2026-09-01, khi Bảng 2 còn theo master
        // TieuChi cũ) tự có đủ dòng khi mở lại, không cần thao tác thủ công.
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

        // Bảng 3.2 — CỐ ĐỊNH (xem TieuChiBang3): 3 dòng (NhomSo=1, Stt 1-3),
        // NHẬP TAY hoàn toàn. Đồng bộ idempotent, cùng style
        // DongBoBang2CoDinhAsync — an toàn gọi lại nhiều lần (tạo phiếu mới
        // lẫn mỗi lần ChiTietAsync).
        private async Task DongBoBang3CoDinhAsync(Phieu4Bang bang3, List<int> nhaThauIds)
        {
            var dongHienTai = await _dongRepository.FindAsync(x => x.BangId == bang3.Id);

            var dongMoi = new List<Phieu4Dong>();
            foreach (var tc in TieuChiBang3)
            {
                if (!dongHienTai.Any(x => x.NhomSo == 1 && x.Stt == tc.Stt))
                    dongMoi.Add(new Phieu4Dong { BangId = bang3.Id, NhomSo = 1, Stt = tc.Stt, NoiDung = tc.Ten, Dvt = "Điểm (1-5)", LoaiDong = "NHAP_TAY" });
            }

            if (dongMoi.Count > 0)
            {
                await _dongRepository.AddRangeAsync(dongMoi);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật cho giá trị con
            }

            // Bù ô giá trị còn thiếu cho từng cột nhà thầu hiện tại — cùng
            // đoạn logic với DongBoBang2CoDinhAsync.
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

        // Bảng 4 — đồng bộ 1 dòng / dbo.DiaDiemNhaAn đang active. Idempotent:
        // chỉ thêm dòng cho DiaDiemNhaAnId CHƯA có trong bảng, không tạo
        // trùng, không tự xóa nếu 1 địa điểm bị tắt active sau đó (giữ lịch
        // sử). Giá trị (Số lượng suất ăn phục vụ/ngày) lưu ở GiaTriChung —
        // bảng này KHÔNG có cột nhà thầu nên không đi qua Phieu4_GiaTri.
        private async Task DongBoBang4TuDiaDiemAsync(Phieu4Bang bang4)
        {
            var diaDiemDangHoatDong = (await _diaDiemNhaAnRepository.FindAsync(x => x.IsActive))
                .OrderBy(x => x.DiaDiem)
                .ToList();
            if (diaDiemDangHoatDong.Count == 0) return;

            var dongHienTai = await _dongRepository.FindAsync(x => x.BangId == bang4.Id);
            var diaDiemIdDaCo = dongHienTai
                .Where(x => x.DiaDiemNhaAnId.HasValue)
                .Select(x => x.DiaDiemNhaAnId!.Value)
                .ToHashSet();
            var stt = dongHienTai.Count == 0 ? 0 : dongHienTai.Max(x => x.Stt ?? 0);

            var dongMoi = new List<Phieu4Dong>();
            foreach (var diaDiem in diaDiemDangHoatDong)
            {
                if (diaDiemIdDaCo.Contains(diaDiem.ID)) continue; // đã đồng bộ rồi — không tạo trùng

                dongMoi.Add(new Phieu4Dong
                {
                    BangId = bang4.Id,
                    Stt = ++stt,
                    NoiDung = diaDiem.DiaDiem,
                    Dvt = "Suất",
                    LoaiDong = "NHAP_TAY",
                    DiaDiemNhaAnId = diaDiem.ID,
                });
            }
            if (dongMoi.Count == 0) return;

            await _dongRepository.AddRangeAsync(dongMoi);
            await _unitOfWork.SaveChangesAsync();
        }

        // Bảng 5 — đồng bộ 1 dòng / cặp (NhaThau, DiaDiemNhaAn) suy ra từ
        // Phiếu 2 trong khoảng ngày của phiếu, qua
        // LayDiaDiemRoRangCuaNhaThauAsync (CHỈ Phiếu 2 — Phiếu 1 không có
        // liên kết nào tới DiaDiemNhaAn, xác nhận nghiệp vụ 2026-09-02).
        // Idempotent: chỉ thêm dòng cho cặp (NhaThauId, DiaDiemNhaAnId) CHƯA
        // có, KHÔNG tự xóa dòng cũ nếu 1 địa điểm sau này không còn "rõ
        // ràng" nữa — giữ lịch sử, giống Bảng 4. Giá trị lưu ở GiaTriChung
        // (nhà thầu đã cố định ở CẤP DÒNG nên không cần chia cột nữa).
        private async Task DongBoBang5TuPhieu2Async(Phieu4Bang bang5, Phieu4TongHop phieu, List<Phieu4NhaThau> danhSachNhaThau)
        {
            var dongHienTai = await _dongRepository.FindAsync(x => x.BangId == bang5.Id);
            var capDaCo = dongHienTai
                .Where(x => x.NhaThauId.HasValue && x.DiaDiemNhaAnId.HasValue)
                .Select(x => (NhaThauId: x.NhaThauId!.Value, DiaDiemNhaAnId: x.DiaDiemNhaAnId!.Value))
                .ToHashSet();
            var stt = dongHienTai.Count == 0 ? 0 : dongHienTai.Max(x => x.Stt ?? 0);

            var tatCaDiaDiemId = new HashSet<int>();
            var capMoi = new List<(int NhaThauId, int DiaDiemNhaAnId)>();
            foreach (var cot in danhSachNhaThau)
            {
                var diaDiemRoRang = await LayDiaDiemRoRangCuaNhaThauAsync(cot.NhaThauId, phieu.TuNgay, phieu.DenNgay);
                foreach (var diaDiemId in diaDiemRoRang)
                {
                    tatCaDiaDiemId.Add(diaDiemId);
                    if (!capDaCo.Contains((cot.NhaThauId, diaDiemId)))
                        capMoi.Add((cot.NhaThauId, diaDiemId));
                }
            }
            if (capMoi.Count == 0) return;

            var tenDiaDiem = (await _diaDiemNhaAnRepository.FindAsync(x => tatCaDiaDiemId.Contains(x.ID)))
                .ToDictionary(x => x.ID, x => x.DiaDiem);

            var dongMoi = new List<Phieu4Dong>();
            foreach (var (nhaThauId, diaDiemId) in capMoi)
            {
                dongMoi.Add(new Phieu4Dong
                {
                    BangId = bang5.Id,
                    Stt = ++stt,
                    NoiDung = tenDiaDiem.TryGetValue(diaDiemId, out var ten) ? ten : $"Địa điểm #{diaDiemId}",
                    Dvt = "Suất",
                    LoaiDong = "NHAP_TAY",
                    DiaDiemNhaAnId = diaDiemId,
                    NhaThauId = nhaThauId,
                });
            }

            await _dongRepository.AddRangeAsync(dongMoi);
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
