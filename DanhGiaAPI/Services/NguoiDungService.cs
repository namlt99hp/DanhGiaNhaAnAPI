using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class NguoiDungService : INguoiDungService
    {
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly INguoiDungVaiTroRepository _nguoiDungVaiTroRepository;
        private readonly IVaiTroRepository _vaiTroRepository;
        private readonly IMauLuongKyRepository _mauLuongKyRepository;
        private readonly INguoiDungMauLuongKyRepository _nguoiDungMauLuongKyRepository;
        private readonly INguoiDungPhieuQuyenRepository _nguoiDungPhieuQuyenRepository;
        private readonly IPhongBanLoaiPhieuRepository _phongBanLoaiPhieuRepository;
        private readonly IQuyenXemPhieuService _quyenXemPhieuService;
        private readonly IChuKyNguoiDungRepository _chuKyNguoiDungRepository;
        private readonly IPhienDangNhapRepository _phienDangNhapRepository;
        private readonly IChuKyPhieuRepository _chuKyPhieuRepository;
        private readonly IPhieu1KiemTraRepository _phieu1Repository;
        private readonly IPhieu2DanhGiaRepository _phieu2Repository;
        private readonly IPhieu3BaoCaoRepository _phieu3Repository;
        private readonly IPhieu4TongHopRepository _phieu4Repository;
        private readonly IQuanTriGuardService _quanTriGuardService;
        private readonly INhaThauRepository _nhaThauRepository;
        private readonly IPhongBanRepository _phongBanRepository;
        private readonly IUnitOfWork _unitOfWork;

        public NguoiDungService(
            INguoiDungRepository nguoiDungRepository,
            INguoiDungVaiTroRepository nguoiDungVaiTroRepository,
            IVaiTroRepository vaiTroRepository,
            IMauLuongKyRepository mauLuongKyRepository,
            INguoiDungMauLuongKyRepository nguoiDungMauLuongKyRepository,
            INguoiDungPhieuQuyenRepository nguoiDungPhieuQuyenRepository,
            IPhongBanLoaiPhieuRepository phongBanLoaiPhieuRepository,
            IQuyenXemPhieuService quyenXemPhieuService,
            IChuKyNguoiDungRepository chuKyNguoiDungRepository,
            IPhienDangNhapRepository phienDangNhapRepository,
            IChuKyPhieuRepository chuKyPhieuRepository,
            IPhieu1KiemTraRepository phieu1Repository,
            IPhieu2DanhGiaRepository phieu2Repository,
            IPhieu3BaoCaoRepository phieu3Repository,
            IPhieu4TongHopRepository phieu4Repository,
            IQuanTriGuardService quanTriGuardService,
            INhaThauRepository nhaThauRepository,
            IPhongBanRepository phongBanRepository,
            IUnitOfWork unitOfWork)
        {
            _nguoiDungRepository = nguoiDungRepository;
            _nguoiDungVaiTroRepository = nguoiDungVaiTroRepository;
            _vaiTroRepository = vaiTroRepository;
            _mauLuongKyRepository = mauLuongKyRepository;
            _nguoiDungMauLuongKyRepository = nguoiDungMauLuongKyRepository;
            _nguoiDungPhieuQuyenRepository = nguoiDungPhieuQuyenRepository;
            _phongBanLoaiPhieuRepository = phongBanLoaiPhieuRepository;
            _quyenXemPhieuService = quyenXemPhieuService;
            _chuKyNguoiDungRepository = chuKyNguoiDungRepository;
            _phienDangNhapRepository = phienDangNhapRepository;
            _chuKyPhieuRepository = chuKyPhieuRepository;
            _phieu1Repository = phieu1Repository;
            _phieu2Repository = phieu2Repository;
            _phieu3Repository = phieu3Repository;
            _phieu4Repository = phieu4Repository;
            _quanTriGuardService = quanTriGuardService;
            _nhaThauRepository = nhaThauRepository;
            _phongBanRepository = phongBanRepository;
            _unitOfWork = unitOfWork;
        }

        // Tài khoản phải rõ ràng thuộc PHÒNG BAN (nội bộ) hoặc NHÀ THẦU, không
        // được cả hai — cần cho đúng nhánh PHONG_BAN/NHA_THAU ở
        // ChuKyPhieuService.KiemTraQuyenKyAsync (xem VaiTro.md).
        private static void KiemTraLoaiTaiKhoan(int? phongBanId, int? nhaThauId)
        {
            if (phongBanId.HasValue && nhaThauId.HasValue)
                throw new ApiException("Tài khoản chỉ được thuộc 1 trong 2: Phòng ban hoặc Nhà thầu, không được cả hai");
        }

        // "Trần" nghiệp vụ cố định cho MỌI tài khoản nhà thầu — không cấu hình
        // được qua UI (khác phòng ban, xem PhongBanLoaiPhieu): nhà thầu chỉ
        // liên quan Phiếu 1, 2, và chỉ ở vai trò KÝ (không bao giờ có Đánh
        // giá/nhập liệu hay Quản lý tiêu chí — xem VaiTro.md mục 10).
        private static readonly HashSet<string> LOAI_PHIEU_NHA_THAU_DUOC_KY = new() { "PHIEU1", "PHIEU2" };
        private static readonly string[] DS_LOAI_PHIEU_TAT_CA = { "PHIEU1", "PHIEU2", "PHIEU3", "PHIEU4" };

        // "Trần" cấu trúc (KHÔNG phải quyền thật) — loại phiếu tài khoản này
        // ĐƯỢC PHÉP cấu hình Đánh giá/Quản lý tiêu chí/Ký. Nhà thầu: cố định
        // {PHIEU1,PHIEU2}. Phòng ban: theo PhongBanLoaiPhieu đã cấu hình, hoặc
        // KHÔNG GIỚI HẠN (cả 4) nếu phòng ban đó chưa cấu hình gì (an toàn khi
        // rollout — xem migration_phongban_loaiphieu.sql).
        private static List<string> TinhLoaiPhieuApDung(NguoiDung nguoiDung, Dictionary<int, List<string>> phongBanLoaiPhieuMap)
        {
            if (nguoiDung.NhaThauId.HasValue)
                return LOAI_PHIEU_NHA_THAU_DUOC_KY.ToList();

            if (nguoiDung.PhongBanId.HasValue)
            {
                var cauHinh = phongBanLoaiPhieuMap.GetValueOrDefault(nguoiDung.PhongBanId.Value) ?? new();
                return cauHinh.Count > 0 ? cauHinh : DS_LOAI_PHIEU_TAT_CA.ToList();
            }

            return DS_LOAI_PHIEU_TAT_CA.ToList();
        }

        private async Task<List<string>> TinhLoaiPhieuApDungAsync(NguoiDung nguoiDung)
        {
            var map = new Dictionary<int, List<string>>();
            if (nguoiDung.PhongBanId.HasValue)
                map[nguoiDung.PhongBanId.Value] = (await _phongBanLoaiPhieuRepository.GetByPhongBanIdAsync(nguoiDung.PhongBanId.Value))
                    .Select(x => x.LoaiPhieu).ToList();

            return TinhLoaiPhieuApDung(nguoiDung, map);
        }

        private async Task<bool> PhieuNamTrongTranAsync(NguoiDung nguoiDung, string loaiPhieu) =>
            (await TinhLoaiPhieuApDungAsync(nguoiDung)).Contains(loaiPhieu);

        public async Task<List<NguoiDungListItemDto>> DanhSachAsync(string? trangThai, int? phongBanId, int? nhaThauId)
        {
            // Nhiều filter tùy chọn -> dùng escape hatch Query() thay vì đẻ
            // 1 method riêng cho từng tổ hợp filter (xem IRepository<T>.Query()).
            var query = _nguoiDungRepository.Query();

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.TrangThai == trangThai);
            if (phongBanId.HasValue)
                query = query.Where(x => x.PhongBanId == phongBanId);
            if (nhaThauId.HasValue)
                query = query.Where(x => x.NhaThauId == nhaThauId);

            var danhSach = query.OrderByDescending(x => x.NgayTao).ToList();
            var ids = danhSach.Select(x => x.Id).ToList();

            var vaiTroMap = await _nguoiDungVaiTroRepository.GetVaiTroMapNhieuNguoiDungAsync(ids);

            // Nạp theo BATCH (không phải N+1) — dữ liệu này cần cho modal
            // "Phân quyền theo Phiếu" mở từ ĐÚNG dòng trong bảng danh sách
            // (QuanLyTaiKhoanPageV2.tsx truyền thẳng row, không gọi lại
            // ChiTietAsync), nên DanhSachAsync bắt buộc phải trả đủ, không chỉ
            // để trống như trước.
            var phieuQuyenMap = _nguoiDungPhieuQuyenRepository.Query()
                .Where(x => ids.Contains(x.NguoiDungId))
                .ToList()
                .GroupBy(x => x.NguoiDungId)
                .ToDictionary(g => g.Key, g => g.Select(x => new NguoiDungPhieuQuyenItemDto
                {
                    LoaiPhieu = x.LoaiPhieu,
                    DuocDanhGia = x.DuocDanhGia,
                    DuocQuanLyTieuChi = x.DuocQuanLyTieuChi
                }).ToList());

            var mauLuongKyMap = _nguoiDungMauLuongKyRepository.Query()
                .Where(x => ids.Contains(x.NguoiDungId))
                .ToList()
                .GroupBy(x => x.NguoiDungId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.MauLuongKyId).ToList());

            // Bảng PhongBanLoaiPhieu rất nhỏ (vài dòng cấu hình) -> nạp hết 1
            // lần rồi tính "trần" của từng dòng trong bộ nhớ, tránh 1 query
            // riêng/user.
            var phongBanLoaiPhieuMap = _phongBanLoaiPhieuRepository.Query()
                .ToList()
                .GroupBy(x => x.PhongBanId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.LoaiPhieu).ToList());

            return danhSach.Select(nd => MapToDto(
                nd,
                vaiTroMap.Where(x => x.NguoiDungId == nd.Id).Select(x => x.Ma).ToList(),
                mauLuongKyMap.GetValueOrDefault(nd.Id, new()),
                phieuQuyenMap.GetValueOrDefault(nd.Id, new()),
                TinhLoaiPhieuApDung(nd, phongBanLoaiPhieuMap)
            )).ToList();
        }

        public async Task<NguoiDungListItemDto> ChiTietAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            var vaiTro = await _nguoiDungVaiTroRepository.GetVaiTroCuaNguoiDungAsync(id);
            var mauLuongKyIds = (await _nguoiDungMauLuongKyRepository.GetByNguoiDungIdAsync(id))
                .Select(x => x.MauLuongKyId).ToList();
            var phieuQuyen = (await _nguoiDungPhieuQuyenRepository.GetByNguoiDungIdAsync(id))
                .Select(x => new NguoiDungPhieuQuyenItemDto
                {
                    LoaiPhieu = x.LoaiPhieu,
                    DuocDanhGia = x.DuocDanhGia,
                    DuocQuanLyTieuChi = x.DuocQuanLyTieuChi
                }).ToList();
            var loaiPhieuApDung = await TinhLoaiPhieuApDungAsync(nguoiDung);
            var danhSachLoaiPhieuDuocXem = await _quyenXemPhieuService.DanhSachLoaiPhieuDuocXemAsync(id);

            return MapToDto(nguoiDung, vaiTro.Select(x => x.Ma).ToList(), mauLuongKyIds, phieuQuyen, loaiPhieuApDung, danhSachLoaiPhieuDuocXem);
        }

        // Admin tạo trực tiếp 1 tài khoản (khác DangKyAsync ở AuthService — tự
        // đăng ký luôn CHO_DUYET): vào thẳng HOAT_DONG, NguoiDuyet/NgayDuyet ghi
        // nhận chính admin đang tạo, coi như đã "tự duyệt" tài khoản do mình tạo.
        public async Task<NguoiDungListItemDto> TaoTaiKhoanAsync(TaoTaiKhoanRequest request, int nguoiTaoId)
        {
            var tenDangNhap = request.TenDangNhap.Trim();

            if (await _nguoiDungRepository.GetByTenDangNhapAsync(tenDangNhap) != null)
                throw new ApiException("Tên đăng nhập đã tồn tại");

            if (!string.IsNullOrWhiteSpace(request.Email) && await _nguoiDungRepository.TonTaiEmailAsync(request.Email))
                throw new ApiException("Email đã được sử dụng");

            KiemTraLoaiTaiKhoan(request.PhongBanId, request.NhaThauId);

            if (request.NhaThauId.HasValue)
            {
                var nhaThau = await _nhaThauRepository.GetByIdAsync(request.NhaThauId.Value)
                    ?? throw new ApiException("Không tìm thấy nhà thầu");
                DanhMucHoatDong.KiemTraNhaThau(nhaThau);
            }
            if (request.PhongBanId.HasValue)
            {
                var phongBan = await _phongBanRepository.GetByIdAsync(request.PhongBanId.Value)
                    ?? throw new ApiException("Không tìm thấy phòng ban");
                DanhMucHoatDong.KiemTraPhongBan(phongBan);
            }

            var nguoiDung = new NguoiDung
            {
                TenDangNhap = tenDangNhap,
                MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword(request.MatKhau),
                HoTen = request.HoTen.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                SoDienThoai = request.SoDienThoai,
                PhongBanId = request.PhongBanId,
                NhaThauId = request.NhaThauId,
                TrangThai = "HOAT_DONG",
                NguoiDuyet = nguoiTaoId,
                NgayDuyet = DateTime.Now,
                NgayTao = DateTime.Now
            };

            await _nguoiDungRepository.AddAsync(nguoiDung);
            await _unitOfWork.SaveChangesAsync();

            if (request.VaiTroIds.Count > 0)
            {
                var idHopLe = await _vaiTroRepository.GetExistingIdsAsync(request.VaiTroIds);
                await _nguoiDungVaiTroRepository.AddRangeAsync(idHopLe.Select(vtId => new NguoiDungVaiTro { NguoiDungId = nguoiDung.Id, VaiTroId = vtId }));
                await _unitOfWork.SaveChangesAsync();
            }

            if (request.MauLuongKyIds.Count > 0)
                await CapNhatLuongKyAsync(nguoiDung.Id, request.MauLuongKyIds);

            if (request.PhieuQuyen.Count > 0)
                await CapNhatPhieuQuyenAsync(nguoiDung.Id, request.PhieuQuyen);

            return await ChiTietAsync(nguoiDung.Id);
        }

        public async Task DuyetAsync(int id, int nguoiDuyetId)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            if (nguoiDung.TrangThai != "CHO_DUYET")
                throw new ApiException("Tài khoản không ở trạng thái chờ duyệt");

            nguoiDung.TrangThai = "HOAT_DONG";
            nguoiDung.NguoiDuyet = nguoiDuyetId;
            nguoiDung.NgayDuyet = DateTime.Now;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task TuChoiAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            if (nguoiDung.TrangThai != "CHO_DUYET")
                throw new ApiException("Chỉ có thể từ chối tài khoản đang chờ duyệt");

            _nguoiDungRepository.Remove(nguoiDung);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task KhoaAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            if (nguoiDung.TrangThai == "CHO_DUYET")
                throw new ApiException("Tài khoản chưa được duyệt, không thể khóa");
            if (nguoiDung.TrangThai == "KHOA")
                throw new ApiException("Tài khoản đã bị khóa từ trước");

            nguoiDung.TrangThai = "KHOA";
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task MoKhoaAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            if (nguoiDung.TrangThai != "KHOA")
                throw new ApiException("Tài khoản không ở trạng thái khóa");

            nguoiDung.TrangThai = "HOAT_DONG";
            await _unitOfWork.SaveChangesAsync();
        }

        // Xóa VĨNH VIỄN — khác TuChoiAsync (chỉ xóa được tài khoản CHO_DUYET,
        // chưa có dấu vết gì). Ở đây tài khoản có thể đã HOAT_DONG/KHOA, nên
        // phải chặn nếu:
        //  - đang xóa chính tài khoản mình đang đăng nhập;
        //  - xóa xong sẽ không còn ai quản lý được tài khoản trong hệ thống
        //    (tái dùng IQuanTriGuardService, mô phỏng bằng "rút hết vai trò");
        //  - tài khoản đã có dấu vết THẬT (đã lập hoặc đã ký ít nhất 1 phiếu)
        //    — xóa sẽ để lại NguoiTao/NguoiKyId mồ côi trên phiếu cũ, nên bắt
        //    khóa (KhoaAsync) thay vì xóa trong trường hợp này.
        // Nếu qua được các chặn trên, dọn sạch mọi bảng con THUỘC SỞ HỮU tài
        // khoản (vai trò, phân quyền ký/phiếu, chữ ký mẫu, phiên đăng nhập)
        // trước khi xóa dòng NguoiDung.
        public async Task XoaVinhVienAsync(int id, int nguoiThucHienId)
        {
            var nguoiDung = await TimHoacLoiAsync(id);

            if (id == nguoiThucHienId)
                throw new ApiException("Không thể tự xóa chính tài khoản đang đăng nhập");

            await _quanTriGuardService.KiemTraSauKhiDoiVaiTroNguoiDungAsync(id, new List<int>());

            var daCoDauVet =
                await _phieu1Repository.AnyAsync(x => x.NguoiTao == id) ||
                await _phieu2Repository.AnyAsync(x => x.NguoiTao == id) ||
                await _phieu3Repository.AnyAsync(x => x.NguoiTao == id) ||
                await _phieu4Repository.AnyAsync(x => x.NguoiTao == id) ||
                await _chuKyPhieuRepository.AnyAsync(x => x.NguoiKyId == id);
            if (daCoDauVet)
                throw new ApiException("Không thể xóa vĩnh viễn vì tài khoản đã có hoạt động trong hệ thống (đã lập/ký phiếu) — hãy khóa tài khoản thay vì xóa.");

            _nguoiDungVaiTroRepository.RemoveRange(await _nguoiDungVaiTroRepository.GetByNguoiDungIdAsync(id));
            _nguoiDungMauLuongKyRepository.RemoveRange(await _nguoiDungMauLuongKyRepository.GetByNguoiDungIdAsync(id));
            _nguoiDungPhieuQuyenRepository.RemoveRange(await _nguoiDungPhieuQuyenRepository.GetByNguoiDungIdAsync(id));
            _chuKyNguoiDungRepository.RemoveRange(await _chuKyNguoiDungRepository.GetByNguoiDungIdAsync(id));
            _phienDangNhapRepository.RemoveRange(await _phienDangNhapRepository.FindAsync(x => x.NguoiDungId == id));

            _nguoiDungRepository.Remove(nguoiDung);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CapNhatVaiTroAsync(int id, List<int> vaiTroIds)
        {
            await TimHoacLoiAsync(id);

            var idHopLe = await _vaiTroRepository.GetExistingIdsAsync(vaiTroIds);

            // Kiểm tra TRƯỚC khi ghi — chặn nếu rút vai trò này khiến không còn
            // ai quản lý được tài khoản trong hệ thống (xem VaiTro.md).
            await _quanTriGuardService.KiemTraSauKhiDoiVaiTroNguoiDungAsync(id, idHopLe);

            var hienTai = await _nguoiDungVaiTroRepository.GetByNguoiDungIdAsync(id);
            _nguoiDungVaiTroRepository.RemoveRange(hienTai);
            await _nguoiDungVaiTroRepository.AddRangeAsync(idHopLe.Select(vtId => new NguoiDungVaiTro { NguoiDungId = id, VaiTroId = vtId }));

            await _unitOfWork.SaveChangesAsync();
        }

        // Mật khẩu mặc định khi Admin reset mật khẩu cho tài khoản khác (nhớ
        // đổi lại sau khi đăng nhập — chưa có cờ "bắt buộc đổi mật khẩu").
        private const string MatKhauMacDinh = "HPDQ@1234";

        public async Task<NguoiDungListItemDto> CapNhatProfileAsync(int id, CapNhatProfileRequest request)
        {
            var nguoiDung = await TimHoacLoiAsync(id);

            var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            if (email != null && email != nguoiDung.Email && await _nguoiDungRepository.TonTaiEmailAsync(email))
                throw new ApiException("Email đã được sử dụng");

            nguoiDung.HoTen = request.HoTen.Trim();
            nguoiDung.Email = email;
            nguoiDung.SoDienThoai = request.SoDienThoai;
            await _unitOfWork.SaveChangesAsync();

            return await ChiTietAsync(id);
        }

        public async Task DoiMatKhauAsync(int id, DoiMatKhauRequest request)
        {
            var nguoiDung = await TimHoacLoiAsync(id);

            if (!BCrypt.Net.BCrypt.Verify(request.MatKhauHienTai, nguoiDung.MatKhauMaHoa))
                throw new ApiException("Mật khẩu hiện tại không đúng");

            nguoiDung.MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword(request.MatKhauMoi);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ResetMatKhauAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            nguoiDung.MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword(MatKhauMacDinh);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<NguoiDung> TimHoacLoiAsync(int id)
        {
            return await _nguoiDungRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy tài khoản", StatusCodes.Status404NotFound);
        }

        private static NguoiDungListItemDto MapToDto(
            NguoiDung nd,
            List<string> maVaiTro,
            List<int>? mauLuongKyIds = null,
            List<NguoiDungPhieuQuyenItemDto>? phieuQuyen = null,
            List<string>? loaiPhieuApDung = null,
            List<string>? danhSachLoaiPhieuDuocXem = null) => new()
        {
            Id = nd.Id,
            TenDangNhap = nd.TenDangNhap,
            HoTen = nd.HoTen,
            Email = nd.Email,
            SoDienThoai = nd.SoDienThoai,
            PhongBanId = nd.PhongBanId,
            NhaThauId = nd.NhaThauId,
            TrangThai = nd.TrangThai,
            NguoiDuyet = nd.NguoiDuyet,
            NgayDuyet = nd.NgayDuyet,
            NgayTao = nd.NgayTao,
            DanhSachVaiTro = maVaiTro,
            DanhSachMauLuongKyId = mauLuongKyIds ?? new(),
            PhieuQuyen = phieuQuyen ?? new(),
            LoaiPhieuApDung = loaiPhieuApDung ?? DS_LOAI_PHIEU_TAT_CA.ToList(),
            DanhSachLoaiPhieuDuocXem = danhSachLoaiPhieuDuocXem ?? new()
        };

        // "Đủ điều kiện CẤU TRÚC" để gán user này vào 1 bước ký cụ thể — không
        // phải là "được phép ký" (đó là việc của NguoiDungMauLuongKy, gán
        // tường minh). PHONG_BAN/NHA_THAU cần user thuộc đúng nhóm; NHA_THAU
        // chỉ kiểm tra được "là tài khoản nhà thầu" (chưa biết nhà thầu nào
        // khớp phiếu cụ thể — việc đó xác định lúc ký thật, xem
        // ChuKyPhieuService.KiemTraQuyenKyAsync); TRUC_TIEP không ràng buộc gì.
        private static bool DuDieuKienCauTruc(MauLuongKy buoc, NguoiDung nguoiDung) => buoc.LoaiNguoiKy switch
        {
            "PHONG_BAN" => nguoiDung.PhongBanId.HasValue && nguoiDung.PhongBanId == buoc.PhongBanId,
            "NHA_THAU" => nguoiDung.NhaThauId.HasValue,
            _ => true // TRUC_TIEP
        };

        // Danh sách bước ký (MauLuongKy) mà user này CÓ THỂ được gán, kèm cờ
        // đã gán chưa — dùng cho khối "Phân quyền theo Phiếu" ở FE.
        public async Task<List<MauLuongKyKhaDungDto>> LuongKyKhaDungAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            var tatCaBuoc = _mauLuongKyRepository.Query()
                .OrderBy(x => x.LoaiPhieu).ThenBy(x => x.BuocThuTu).ToList();
            var daGan = (await _nguoiDungMauLuongKyRepository.GetByNguoiDungIdAsync(id))
                .Select(x => x.MauLuongKyId).ToHashSet();
            var loaiPhieuApDung = await TinhLoaiPhieuApDungAsync(nguoiDung);

            return tatCaBuoc.Select(b => new MauLuongKyKhaDungDto
            {
                MauLuongKyId = b.Id,
                LoaiPhieu = b.LoaiPhieu,
                BuocThuTu = b.BuocThuTu,
                TenBuoc = b.TenBuoc,
                LoaiNguoiKy = b.LoaiNguoiKy,
                DuDieuKienCauTruc = DuDieuKienCauTruc(b, nguoiDung),
                DuDieuKienPhongBan = loaiPhieuApDung.Contains(b.LoaiPhieu),
                DaDuocGan = daGan.Contains(b.Id)
            }).ToList();
        }

        // Thay thế TOÀN BỘ tập bước ký được gán trực tiếp cho 1 tài khoản —
        // cùng kiểu với CapNhatVaiTroAsync (gửi thiếu 1 Id = mất quyền ký bước
        // đó). Chặn gán vào bước không đủ điều kiện cấu trúc (VD gán user nhà
        // thầu vào bước PHONG_BAN nội bộ) để tránh cấu hình vô nghĩa.
        public async Task CapNhatLuongKyAsync(int id, List<int> mauLuongKyIds)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            var idHopLe = await _mauLuongKyRepository.GetExistingIdsAsync(mauLuongKyIds);

            var cacBuoc = _mauLuongKyRepository.Query().Where(x => idHopLe.Contains(x.Id)).ToList();
            foreach (var buoc in cacBuoc)
            {
                if (!DuDieuKienCauTruc(buoc, nguoiDung))
                    throw new ApiException(
                        $"Tài khoản không đủ điều kiện phòng ban/nhà thầu để gán vào bước \"{buoc.TenBuoc}\" ({buoc.LoaiPhieu})");

                if (!await PhieuNamTrongTranAsync(nguoiDung, buoc.LoaiPhieu))
                    throw new ApiException(
                        $"Phòng ban/Nhà thầu của tài khoản không xử lý {buoc.LoaiPhieu} — không thể gán ký bước \"{buoc.TenBuoc}\". " +
                        "Nếu cần mở rộng, cấu hình lại ở màn \"Phòng ban\".");
            }

            var hienTai = await _nguoiDungMauLuongKyRepository.GetByNguoiDungIdAsync(id);
            _nguoiDungMauLuongKyRepository.RemoveRange(hienTai);
            await _nguoiDungMauLuongKyRepository.AddRangeAsync(
                idHopLe.Select(mlkId => new NguoiDungMauLuongKy { NguoiDungId = id, MauLuongKyId = mlkId }));

            await _unitOfWork.SaveChangesAsync();
        }

        // Thay thế TOÀN BỘ quyền thao tác nội dung phiếu (đánh giá/quản lý
        // tiêu chí) của 1 tài khoản — hoàn toàn độc lập với quyền ký
        // (CapNhatLuongKyAsync) và quyền quản trị (CapNhatVaiTroAsync).
        public async Task CapNhatPhieuQuyenAsync(int id, List<NguoiDungPhieuQuyenItemDto> danhSach)
        {
            var nguoiDung = await TimHoacLoiAsync(id);

            foreach (var item in danhSach.Where(x => x.DuocDanhGia || x.DuocQuanLyTieuChi))
            {
                // Nhà thầu chỉ được cấp quyền KÝ (CapNhatLuongKyAsync) — không
                // bao giờ có Đánh giá/nhập liệu hay Quản lý tiêu chí, bất kể
                // loại phiếu nào (xem VaiTro.md mục 10).
                if (nguoiDung.NhaThauId.HasValue)
                    throw new ApiException(
                        "Tài khoản nhà thầu chỉ được cấp quyền Ký (hoặc từ chối) — không được Đánh giá/nhập liệu hoặc Quản lý tiêu chí.");

                if (!await PhieuNamTrongTranAsync(nguoiDung, item.LoaiPhieu.Trim()))
                    throw new ApiException(
                        $"Phòng ban của tài khoản không xử lý {item.LoaiPhieu} — không thể cấp quyền Đánh giá/nhập liệu " +
                        "hoặc Quản lý tiêu chí cho loại phiếu này. Nếu cần mở rộng, cấu hình lại ở màn \"Phòng ban\".");
            }

            var hienTai = await _nguoiDungPhieuQuyenRepository.GetByNguoiDungIdAsync(id);
            _nguoiDungPhieuQuyenRepository.RemoveRange(hienTai);

            var moi = danhSach
                .Where(x => x.DuocDanhGia || x.DuocQuanLyTieuChi)
                .Select(x => new NguoiDungPhieuQuyen
                {
                    NguoiDungId = id,
                    LoaiPhieu = x.LoaiPhieu.Trim(),
                    DuocDanhGia = x.DuocDanhGia,
                    DuocQuanLyTieuChi = x.DuocQuanLyTieuChi
                });
            await _nguoiDungPhieuQuyenRepository.AddRangeAsync(moi);

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
