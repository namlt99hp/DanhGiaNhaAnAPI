using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Phieu2;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    // Xem quyết định thiết kế tại 02. Phantich/modules/Phieu2_DanhGiaSuatAn.md
    // Phiếu 2 gắn với Bếp ăn + 1 HOẶC NHIỀU Nhà ăn (bảng liên kết Phieu2_NhaAn,
    // xem Entities/Phieu2NhaAn.cs — đánh giá 1 lần cho nhiều nhà ăn của cùng
    // bếp ăn); Phiếu 1 liên kết là TÙY CHỌN (Phieu1Id NULL nếu ngày đó không
    // lập Phiếu 1 cho bếp ăn).
    // 5 tiêu chí cố định; riêng VSATTP tự lấy điểm từ Phieu1_KetLuan.DiemDanhGia
    // khi có Phiếu 1 liên kết, ngược lại để trống cho nhập tay.
    public class Phieu2Service : IPhieu2Service
    {
        // 5 tiêu chí cố định (thứ tự ưu tiên = ThuTu)
        private static readonly List<(string Ma, string Ten, int ThuTu)> TieuChiCoDinh = new()
        {
            ("VSATTP",               "Tuân thủ điều kiện vệ sinh an toàn thực phẩm",  0),
            ("DINH_LUONG_THUC_DON",  "Thực đơn và định lượng suất ăn",                1),
            ("THAI_DO_PHOI_HOP",     "Thái độ phục vụ và phối hợp",                   2),
            ("PHAN_HOI_SU_CO",       "Xử lý phản hồi và sự cố",                       3),
            ("DIEU_KHOAN_KHAC",      "Các điều khoản thỏa thuận khác",                4),
        };

        private readonly IPhieu2DanhGiaRepository     _phieu2Repository;
        private readonly IPhieu2TieuChiRepository     _tieuChiRepository;
        private readonly IPhieu2KetQuaRepository      _ketQuaRepository;
        private readonly IPhieu2YKienNhaThauRepository _yKienRepository;
        private readonly IPhieu1KiemTraRepository     _phieu1Repository;
        private readonly IPhieu1KetLuanRepository     _phieu1KetLuanRepository;
        private readonly INhaThauRepository           _nhaThauRepository;
        private readonly IDiaDiemNhaAnRepository      _diaDiemNhaAnRepository;
        private readonly IPhieu2NhaAnRepository       _phieu2NhaAnRepository;
        private readonly INguoiDungPhieuQuyenRepository _nguoiDungPhieuQuyenRepository;
        private readonly ISoHieuService               _soHieuService;
        private readonly ITepDinhKemService           _tepDinhKemService;
        private readonly IChuKyPhieuService           _chuKyPhieuService;
        private readonly IUnitOfWork                  _unitOfWork;

        public Phieu2Service(
            IPhieu2DanhGiaRepository     phieu2Repository,
            IPhieu2TieuChiRepository     tieuChiRepository,
            IPhieu2KetQuaRepository      ketQuaRepository,
            IPhieu2YKienNhaThauRepository yKienRepository,
            IPhieu1KiemTraRepository     phieu1Repository,
            IPhieu1KetLuanRepository     phieu1KetLuanRepository,
            INhaThauRepository           nhaThauRepository,
            IDiaDiemNhaAnRepository      diaDiemNhaAnRepository,
            IPhieu2NhaAnRepository       phieu2NhaAnRepository,
            INguoiDungPhieuQuyenRepository nguoiDungPhieuQuyenRepository,
            ISoHieuService               soHieuService,
            ITepDinhKemService           tepDinhKemService,
            IChuKyPhieuService           chuKyPhieuService,
            IUnitOfWork                  unitOfWork)
        {
            _phieu2Repository        = phieu2Repository;
            _tieuChiRepository       = tieuChiRepository;
            _ketQuaRepository        = ketQuaRepository;
            _yKienRepository         = yKienRepository;
            _phieu1Repository        = phieu1Repository;
            _phieu1KetLuanRepository = phieu1KetLuanRepository;
            _nhaThauRepository       = nhaThauRepository;
            _diaDiemNhaAnRepository  = diaDiemNhaAnRepository;
            _phieu2NhaAnRepository   = phieu2NhaAnRepository;
            _nguoiDungPhieuQuyenRepository = nguoiDungPhieuQuyenRepository;
            _soHieuService           = soHieuService;
            _tepDinhKemService       = tepDinhKemService;
            _chuKyPhieuService       = chuKyPhieuService;
            _unitOfWork              = unitOfWork;
        }

        // Quyền "Đánh giá / nhập liệu" theo NguoiDungPhieuQuyen (xem
        // 02. Phantich/modules/VaiTro.md mục 9) — KHÔNG hard-code theo phòng
        // ban, để Admin tự cấp linh hoạt (đúng tinh thần "Người đánh giá có
        // thể thuộc P.ĐN/P.ATMT/khác" ở PhanTichNghiepVu.md).
        // laAdmin bypass, giống hệt CoQuyen() ở Program.cs / coQuyen() ở FE
        // (quyenV2.ts) — admin (VaiTro.LaQuanTriVien) luôn được phép mọi thao
        // tác, không bị chặn bởi NguoiDungPhieuQuyen.
        private async Task KiemTraQuyenDanhGiaAsync(int? nguoiDungId, bool laAdmin)
        {
            if (laAdmin) return;

            if (!nguoiDungId.HasValue || !await _nguoiDungPhieuQuyenRepository.AnyAsync(
                    x => x.NguoiDungId == nguoiDungId.Value && x.LoaiPhieu == "PHIEU2" && x.DuocDanhGia))
                throw new ApiException(
                    "Bạn không có quyền tạo Phiếu 2 — liên hệ Admin để được phân quyền \"Đánh giá / nhập liệu\" ở mục Phân quyền theo Phiếu",
                    StatusCodes.Status403Forbidden);
        }

        // ============================================================
        // DANH SÁCH
        // ============================================================

        public async Task<List<Phieu2DanhSachItemDto>> DanhSachAsync(
            int? nhaThauId, int? bepAnId, int? thang, int? nam, string? trangThai, int? nhaThauCuaNguoiGoi)
        {
            var query = _phieu2Repository.Query();

            if (nhaThauCuaNguoiGoi.HasValue) query = query.Where(x => x.NhaThauId == nhaThauCuaNguoiGoi);
            else if (nhaThauId.HasValue)     query = query.Where(x => x.NhaThauId == nhaThauId);
            if (bepAnId.HasValue)   query = query.Where(x => x.BepAnId   == bepAnId);
            if (thang.HasValue)     query = query.Where(x => x.Thang     == thang);
            if (nam.HasValue)       query = query.Where(x => x.Nam       == nam);
            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.TrangThai == trangThai);

            var danhSach = query.OrderByDescending(x => x.Nam)
                        .ThenByDescending(x => x.Thang)
                        .ThenByDescending(x => x.Id)
                        .ToList();

            var phieuIds = danhSach.Select(x => x.Id).ToList();
            var lienKet = phieuIds.Count > 0
                ? await _phieu2NhaAnRepository.FindAsync(x => phieuIds.Contains(x.PhieuId))
                : new List<Phieu2NhaAn>();
            var nhaAnTheoPhieu = lienKet.GroupBy(x => x.PhieuId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.NhaAnId).ToList());

            return danhSach.Select(p => new Phieu2DanhSachItemDto
            {
                Id = p.Id,
                SoHieu = p.SoHieu,
                Thang = p.Thang,
                Nam = p.Nam,
                NhaThauId = p.NhaThauId,
                BepAnId = p.BepAnId,
                NhaAnIds = nhaAnTheoPhieu.TryGetValue(p.Id, out var ids) ? ids : new List<int>(),
                ThoiGianTu = p.ThoiGianTu,
                ThoiGianDen = p.ThoiGianDen,
                DiaDiem = p.DiaDiem,
                ThoiGianKiemTraText = p.ThoiGianKiemTraText,
                Phieu1Id = p.Phieu1Id,
                NguoiTao = p.NguoiTao,
                TrangThai = p.TrangThai,
                NgayTao = p.NgayTao,
            }).ToList();
        }

        // ============================================================
        // CHI TIẾT
        // ============================================================

        public async Task<Phieu2ResponseDto> ChiTietAsync(int id, int? nhaThauCuaNguoiGoi)
        {
            var phieu = await _phieu2Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu đánh giá", StatusCodes.Status404NotFound);

            if (nhaThauCuaNguoiGoi.HasValue && phieu.NhaThauId != nhaThauCuaNguoiGoi)
                throw new ApiException("Bạn không có quyền xem phiếu của nhà thầu khác", StatusCodes.Status403Forbidden);

            var tieuChi       = (await _tieuChiRepository.FindAsync(x => x.PhieuId == id))
                                .OrderBy(x => x.ThuTu).ToList();
            var ketQua        = await _ketQuaRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            var yKien         = await _yKienRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            var phieu1        = phieu.Phieu1Id.HasValue ? await _phieu1Repository.GetByIdAsync(phieu.Phieu1Id.Value) : null;
            var phieu1KetLuan = phieu.Phieu1Id.HasValue
                ? await _phieu1KetLuanRepository.FirstOrDefaultAsync(x => x.PhieuId == phieu.Phieu1Id.Value)
                : null;
            var nhaAnIds      = (await _phieu2NhaAnRepository.FindAsync(x => x.PhieuId == id))
                                .Select(x => x.NhaAnId).ToList();
            var danhSachNhaAn = nhaAnIds.Count > 0
                ? await _diaDiemNhaAnRepository.FindAsync(x => nhaAnIds.Contains(x.ID))
                : new List<DiaDiemNhaAn>();

            return new Phieu2ResponseDto
            {
                Phieu         = phieu,
                TieuChi       = tieuChi,
                KetQua        = ketQua,
                YKienNhaThau  = yKien,
                Phieu1        = phieu1,
                Phieu1KetLuan = phieu1KetLuan,
                DanhSachNhaAn = danhSachNhaAn,
            };
        }

        // ============================================================
        // TẠO MỚI
        // ============================================================

        public async Task<Phieu2ResponseDto> ThemAsync(Phieu2Request request, int? nguoiTaoId, bool laAdmin)
        {
            await KiemTraQuyenDanhGiaAsync(nguoiTaoId, laAdmin);

            // Validate nhà ăn — cho chọn NHIỀU nhà ăn (đánh giá 1 lần cho nhiều
            // nhà ăn của cùng bếp ăn), không ràng buộc theo BepAnId (chưa có
            // liên kết chính thức Bếp ăn <-> Nhà ăn, xem comment đầu class).
            var nhaAnIds = await KiemTraNhaAnAsync(request.NhaAnIds);

            // Validate Phiếu 1 — không bắt buộc: ngày đó có thể chỉ kiểm tra nhà
            // ăn mà không lập Phiếu 1, khi đó điểm VSATTP để trống, nhập tay sau.
            Phieu1KetLuan? phieu1KetLuan = null;
            if (request.Phieu1Id.HasValue)
            {
                var phieu1 = await _phieu1Repository.GetByIdAsync(request.Phieu1Id.Value)
                    ?? throw new ApiException("Không tìm thấy phiếu kiểm tra VSATTP (Phiếu 1) liên kết");
                if (phieu1.NhaThauId != request.NhaThauId)
                    throw new ApiException("Phiếu 1 không thuộc về nhà thầu đã chọn");

                phieu1KetLuan = await _phieu1KetLuanRepository.FirstOrDefaultAsync(x => x.PhieuId == phieu1.Id);
            }

            // Validate nhà thầu
            var nhaThau = await _nhaThauRepository.GetByIdAsync(request.NhaThauId)
                ?? throw new ApiException("Không tìm thấy nhà thầu");

            // Sinh số hiệu: {seq:003}/{năm}/PĐGCLDVSA
            var soHieu = await SinhSoHieuAsync(request.Nam);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Lưu phiếu chính
                var phieu = new Phieu2DanhGia
                {
                    SoHieu      = soHieu,
                    Thang       = request.Thang,
                    Nam         = request.Nam,
                    NhaThauId   = request.NhaThauId,
                    BepAnId     = request.BepAnId,
                    ThoiGianTu  = request.ThoiGianTu,
                    ThoiGianDen = request.ThoiGianDen,
                    DiaDiem     = request.DiaDiem,
                    ThoiGianKiemTraText = request.ThoiGianKiemTraText,
                    Phieu1Id    = request.Phieu1Id,
                    NguoiTao    = nguoiTaoId,
                    TrangThai   = "NHAP",
                    NgayTao     = DateTime.Now,
                };
                await _phieu2Repository.AddAsync(phieu);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật trước khi ghi tiêu chí/nhà ăn

                // Lưu danh sách nhà ăn
                foreach (var nhaAnId in nhaAnIds.Select(x => x.ID))
                    await _phieu2NhaAnRepository.AddAsync(new Phieu2NhaAn { PhieuId = phieu.Id, NhaAnId = nhaAnId });
                await _unitOfWork.SaveChangesAsync();

                // Lưu 5 tiêu chí
                var tieuChiEntities = XayDungTieuChi(phieu.Id, request.TieuChi, phieu1KetLuan);
                foreach (var tc in tieuChiEntities)
                    await _tieuChiRepository.AddAsync(tc);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật để chốt ảnh

                // Chốt liên kết ảnh TinyMCE
                var tieuChiDaLuu = await _tieuChiRepository.FindAsync(x => x.PhieuId == phieu.Id);
                foreach (var tc in tieuChiDaLuu)
                    await _tepDinhKemService.ChotLienKetCkeditorAsync(tc.Id, tc.GhiChu);

                // Tính kết quả tổng hợp
                var soTieuChiDat = tieuChiEntities.Count(x => x.Dat);
                var ketQua = new Phieu2KetQua
                {
                    PhieuId       = phieu.Id,
                    SoTieuChiDat  = soTieuChiDat,
                    TongSoTieuChi = 5,
                };
                await _ketQuaRepository.AddAsync(ketQua);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                return await ChiTietAsync(phieu.Id, null);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================================
        // CẬP NHẬT
        // ============================================================

        public async Task<Phieu2ResponseDto> SuaAsync(int id, Phieu2Request request)
        {
            var phieu = await _phieu2Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu đánh giá", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể sửa phiếu ở trạng thái Nháp hoặc Từ chối");

            // Validate nhà ăn — cho chọn nhiều (xem ThemAsync)
            var nhaAnIds = await KiemTraNhaAnAsync(request.NhaAnIds);

            // Validate Phiếu 1 — không bắt buộc (xem ThemAsync)
            Phieu1KetLuan? phieu1KetLuan = null;
            if (request.Phieu1Id.HasValue)
            {
                var phieu1 = await _phieu1Repository.GetByIdAsync(request.Phieu1Id.Value)
                    ?? throw new ApiException("Không tìm thấy phiếu kiểm tra VSATTP (Phiếu 1) liên kết");
                phieu1KetLuan = await _phieu1KetLuanRepository.FirstOrDefaultAsync(x => x.PhieuId == phieu1.Id);
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Cập nhật phiếu chính
                phieu.BepAnId     = request.BepAnId;
                phieu.ThoiGianTu  = request.ThoiGianTu;
                phieu.ThoiGianDen = request.ThoiGianDen;
                phieu.DiaDiem     = request.DiaDiem;
                phieu.ThoiGianKiemTraText = request.ThoiGianKiemTraText;
                phieu.Phieu1Id    = request.Phieu1Id;
                _phieu2Repository.Update(phieu);
                await _unitOfWork.SaveChangesAsync();

                // Xóa danh sách nhà ăn cũ, thêm mới
                var nhaAnCu = await _phieu2NhaAnRepository.FindAsync(x => x.PhieuId == id);
                _phieu2NhaAnRepository.RemoveRange(nhaAnCu);
                await _unitOfWork.SaveChangesAsync();

                foreach (var nhaAnId in nhaAnIds.Select(x => x.ID))
                    await _phieu2NhaAnRepository.AddAsync(new Phieu2NhaAn { PhieuId = id, NhaAnId = nhaAnId });
                await _unitOfWork.SaveChangesAsync();

                // Xóa tiêu chí cũ, thêm mới
                var tieuChiCu = await _tieuChiRepository.FindAsync(x => x.PhieuId == id);
                _tieuChiRepository.RemoveRange(tieuChiCu);
                await _unitOfWork.SaveChangesAsync();

                var tieuChiMoi = XayDungTieuChi(id, request.TieuChi, phieu1KetLuan);
                foreach (var tc in tieuChiMoi)
                    await _tieuChiRepository.AddAsync(tc);
                await _unitOfWork.SaveChangesAsync();

                // Chốt liên kết ảnh TinyMCE
                var tieuChiDaLuu = await _tieuChiRepository.FindAsync(x => x.PhieuId == id);
                foreach (var tc in tieuChiDaLuu)
                    await _tepDinhKemService.ChotLienKetCkeditorAsync(tc.Id, tc.GhiChu);

                // Cập nhật kết quả tổng hợp
                var ketQua = await _ketQuaRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
                var soTieuChiDat = tieuChiMoi.Count(x => x.Dat);
                if (ketQua == null)
                {
                    ketQua = new Phieu2KetQua { PhieuId = id, TongSoTieuChi = 5 };
                    await _ketQuaRepository.AddAsync(ketQua);
                }
                ketQua.SoTieuChiDat = soTieuChiDat;
                _ketQuaRepository.Update(ketQua);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();
                return await ChiTietAsync(id, null);
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
            var phieu = await _phieu2Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu đánh giá", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP")
                throw new ApiException("Chỉ có thể xóa phiếu ở trạng thái Nháp");

            var tieuChi = await _tieuChiRepository.FindAsync(x => x.PhieuId == id);
            _tieuChiRepository.RemoveRange(tieuChi);

            var nhaAn = await _phieu2NhaAnRepository.FindAsync(x => x.PhieuId == id);
            _phieu2NhaAnRepository.RemoveRange(nhaAn);

            var ketQua = await _ketQuaRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            if (ketQua != null) _ketQuaRepository.Remove(ketQua);

            var yKien = await _yKienRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            if (yKien != null) _yKienRepository.Remove(yKien);

            _phieu2Repository.Remove(phieu);
            await _unitOfWork.SaveChangesAsync();
        }

        // ============================================================
        // GỬI KÝ
        // ============================================================

        public async Task<Phieu2DanhGia> GuiKyAsync(int id)
        {
            var phieu = await _phieu2Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu đánh giá", StatusCodes.Status404NotFound);
            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Phiếu không ở trạng thái phù hợp để gửi ký");

            await _chuKyPhieuService.KhoiTaoLuongKyAsync("PHIEU2", id);
            phieu.TrangThai = "CHO_KY";
            _phieu2Repository.Update(phieu);
            await _unitOfWork.SaveChangesAsync();
            return phieu;
        }

        // ============================================================
        // ĐỒNG BỘ TRẠNG THÁI SAU KHI KÝ / TỪ CHỐI
        // ============================================================

        public async Task<Phieu2DanhGia> DongBoTrangThaiAsync(int id)
        {
            var phieu = await _phieu2Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu đánh giá", StatusCodes.Status404NotFound);

            var trangThaiMoi = await _chuKyPhieuService.TrangThaiTongAsync("PHIEU2", id);
            if (trangThaiMoi is "DA_DUYET" or "TU_CHOI" or "CHO_KY")
            {
                phieu.TrangThai = trangThaiMoi;
                _phieu2Repository.Update(phieu);
                await _unitOfWork.SaveChangesAsync();
            }
            return phieu;
        }

        // ============================================================
        // Ý KIẾN PHẢN HỒI NHÀ THẦU
        // ============================================================

        public async Task<Phieu2ResponseDto> PhanHoiYKienNhaThauAsync(int id, Phieu2YKienNhaThauRequest request)
        {
            var phieu = await _phieu2Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu đánh giá", StatusCodes.Status404NotFound);

            var yKien = await _yKienRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            if (yKien == null)
            {
                yKien = new Phieu2YKienNhaThau { PhieuId = id };
                await _yKienRepository.AddAsync(yKien);
                await _unitOfWork.SaveChangesAsync(); // cần Id để chốt ảnh
            }
            yKien.YKien        = request.YKien;
            yKien.NguoiPhanHoi = request.NguoiPhanHoi;
            yKien.NgayPhanHoi  = request.NgayPhanHoi;
            _yKienRepository.Update(yKien);
            await _unitOfWork.SaveChangesAsync();

            await _tepDinhKemService.ChotLienKetCkeditorAsync(yKien.Id, yKien.YKien);

            return await ChiTietAsync(id, null);
        }

        // ============================================================
        // DANH SÁCH PHIẾU 1 KHẢ DỤNG ĐỂ CHỌN LIÊN KẾT
        // ============================================================

        public async Task<List<Phieu1KiemTra>> DanhSachPhieu1KhaDungAsync(int nhaThauId, int? bepAnId)
        {
            var danhSach = (await _phieu1Repository.FindAsync(x => x.NhaThauId == nhaThauId)).AsEnumerable();
            if (bepAnId.HasValue)
                danhSach = danhSach.Where(x => x.BepAnId == bepAnId.Value);

            return danhSach.OrderByDescending(x => x.NgayKiemTra).ToList();
        }

        // ============================================================
        // HELPER: Sinh số hiệu
        // ============================================================

        // Quy ước đánh số Phiếu 2 (Bảng đánh giá chất lượng dịch vụ suất ăn):
        // {seq:003}/{năm}/PĐGCLDVSA — 1 dãy số DUY NHẤT dùng chung cho toàn bộ
        // nhà thầu, bắt đầu từ 001 ngày 1/1 và reset lại vào ngày 1/1 năm sau
        // (KHÔNG tách theo nhà thầu, KHÔNG reset theo tháng).
        private async Task<string> SinhSoHieuAsync(int nam)
        {
            var seq = await _soHieuService.SinhSoTiepTheoAsync("PHIEU2", null, nam, null);
            return $"{seq:D3}/{nam}/PĐGCLDVSA";
        }

        // ============================================================
        // HELPER: Nhà ăn (chọn nhiều)
        // ============================================================

        // Validate danh sách Nhà ăn đã chọn — bắt buộc chọn ít nhất 1, mỗi Id
        // phải tồn tại trong danh mục DiaDiemNhaAn. Không ràng buộc theo
        // BepAnId (chưa có liên kết chính thức Bếp ăn <-> Nhà ăn, chọn tự do —
        // xác nhận nghiệp vụ 2026-08-27, xem Phieu2_DanhGiaSuatAn.md).
        private async Task<List<DiaDiemNhaAn>> KiemTraNhaAnAsync(List<int> nhaAnIds)
        {
            var idsHopLe = (nhaAnIds ?? new List<int>()).Distinct().ToList();
            if (idsHopLe.Count == 0)
                throw new ApiException("Vui lòng chọn ít nhất 1 nhà ăn");

            var danhSach = await _diaDiemNhaAnRepository.FindAsync(x => idsHopLe.Contains(x.ID));
            if (danhSach.Count != idsHopLe.Count)
                throw new ApiException("Không tìm thấy 1 hoặc nhiều nhà ăn đã chọn");

            return danhSach;
        }

        // ============================================================
        // HELPER: Xây dựng danh sách 5 tiêu chí từ request
        // ============================================================

        private static List<Phieu2TieuChi> XayDungTieuChi(
            int phieuId,
            List<Phieu2TieuChiRequest> tieuChiRequest,
            Phieu1KetLuan? phieu1KetLuan)
        {
            var result = new List<Phieu2TieuChi>();

            foreach (var mau in TieuChiCoDinh)
            {
                var req = tieuChiRequest.FirstOrDefault(x => x.MaTieuChi == mau.Ma);
                var dat      = req?.Dat ?? false;
                var khongDat = req?.KhongDat ?? false;

                // Riêng VSATTP: nếu Đạt thì lấy điểm từ Phiếu 1
                decimal? diem = null;
                if (mau.Ma == "VSATTP" && dat && phieu1KetLuan?.DiemDanhGia != null)
                    diem = phieu1KetLuan.DiemDanhGia;
                else
                    diem = req?.Diem;

                result.Add(new Phieu2TieuChi
                {
                    PhieuId    = phieuId,
                    MaTieuChi  = mau.Ma,
                    TenTieuChi = req?.TenTieuChi ?? mau.Ten,
                    Dat        = dat,
                    KhongDat   = khongDat,
                    Diem       = diem,
                    GhiChu     = req?.GhiChu,
                    ThuTu      = mau.ThuTu,
                });
            }

            return result;
        }
    }
}
