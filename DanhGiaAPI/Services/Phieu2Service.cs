using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Phieu2;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    // Xem quyết định thiết kế tại 02. Phantich/modules/Phieu2_DanhGiaSuatAn.md
    // Phiếu 2 gắn với Bếp ăn + Nhà ăn (NhaAnId NOT NULL); Phiếu 1 liên kết là
    // TÙY CHỌN (Phieu1Id NULL nếu ngày đó không lập Phiếu 1 cho bếp ăn).
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
            _soHieuService           = soHieuService;
            _tepDinhKemService       = tepDinhKemService;
            _chuKyPhieuService       = chuKyPhieuService;
            _unitOfWork              = unitOfWork;
        }

        // ============================================================
        // DANH SÁCH
        // ============================================================

        public async Task<List<Phieu2DanhGia>> DanhSachAsync(
            int? nhaThauId, int? bepAnId, int? thang, int? nam, string? trangThai)
        {
            var query = _phieu2Repository.Query();

            if (nhaThauId.HasValue) query = query.Where(x => x.NhaThauId == nhaThauId);
            if (bepAnId.HasValue)   query = query.Where(x => x.BepAnId   == bepAnId);
            if (thang.HasValue)     query = query.Where(x => x.Thang     == thang);
            if (nam.HasValue)       query = query.Where(x => x.Nam       == nam);
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

        public async Task<Phieu2ResponseDto> ChiTietAsync(int id)
        {
            var phieu = await _phieu2Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu đánh giá", StatusCodes.Status404NotFound);

            var tieuChi       = (await _tieuChiRepository.FindAsync(x => x.PhieuId == id))
                                .OrderBy(x => x.ThuTu).ToList();
            var ketQua        = await _ketQuaRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            var yKien         = await _yKienRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            var phieu1        = phieu.Phieu1Id.HasValue ? await _phieu1Repository.GetByIdAsync(phieu.Phieu1Id.Value) : null;
            var phieu1KetLuan = phieu.Phieu1Id.HasValue
                ? await _phieu1KetLuanRepository.FirstOrDefaultAsync(x => x.PhieuId == phieu.Phieu1Id.Value)
                : null;
            var nhaAn         = await _diaDiemNhaAnRepository.GetByIdAsync(phieu.NhaAnId);

            return new Phieu2ResponseDto
            {
                Phieu         = phieu,
                TieuChi       = tieuChi,
                KetQua        = ketQua,
                YKienNhaThau  = yKien,
                Phieu1        = phieu1,
                Phieu1KetLuan = phieu1KetLuan,
                NhaAn         = nhaAn,
            };
        }

        // ============================================================
        // TẠO MỚI
        // ============================================================

        public async Task<Phieu2ResponseDto> ThemAsync(Phieu2Request request, int? nguoiTaoId)
        {
            // Validate nhà ăn
            _ = await _diaDiemNhaAnRepository.GetByIdAsync(request.NhaAnId)
                ?? throw new ApiException("Không tìm thấy nhà ăn");

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

            // Sinh số hiệu: DG-{MaNhaThau}-{MM}{YYYY}-{seq:003}
            var soHieu = await SinhSoHieuAsync(nhaThau.Ma, request.Nam, request.Thang);

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
                    NhaAnId     = request.NhaAnId,
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
                await _unitOfWork.SaveChangesAsync(); // cần Id thật trước khi ghi tiêu chí

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
                return await ChiTietAsync(phieu.Id);
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

            // Validate nhà ăn
            _ = await _diaDiemNhaAnRepository.GetByIdAsync(request.NhaAnId)
                ?? throw new ApiException("Không tìm thấy nhà ăn");

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
                phieu.NhaAnId     = request.NhaAnId;
                phieu.ThoiGianTu  = request.ThoiGianTu;
                phieu.ThoiGianDen = request.ThoiGianDen;
                phieu.DiaDiem     = request.DiaDiem;
                phieu.ThoiGianKiemTraText = request.ThoiGianKiemTraText;
                phieu.Phieu1Id    = request.Phieu1Id;
                _phieu2Repository.Update(phieu);
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
                return await ChiTietAsync(id);
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

            return await ChiTietAsync(id);
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

        private async Task<string> SinhSoHieuAsync(string maNhaThau, int nam, int thang)
        {
            var seq = await _soHieuService.SinhSoTiepTheoAsync("PHIEU2", maNhaThau, nam, thang);
            return $"DG-{maNhaThau}-{thang:D2}{nam}-{seq:D3}";
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
