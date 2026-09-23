using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.Common;
using DanhGiaAPI.DTOs.Phieu1;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    // Xem quyết định thiết kế (sửa/xóa được ở trạng thái nào, cách sinh SoHieu,
    // đồng bộ TrangThai với luồng ký...) trong 02. Phantich/modules/Phieu1_KiemTraVSATTP.md
    public class Phieu1Service : IPhieu1Service
    {
        private readonly IPhieu1KiemTraRepository _phieu1Repository;
        private readonly IPhieu1ChiTietRepository _phieu1ChiTietRepository;
        private readonly IPhieu1KetLuanRepository _phieu1KetLuanRepository;
        private readonly IBepAnRepository _bepAnRepository;
        private readonly IPhongBanRepository _phongBanRepository;
        private readonly INhaThauRepository _nhaThauRepository;
        private readonly INguoiDungPhieuQuyenRepository _nguoiDungPhieuQuyenRepository;
        private readonly IQuyenXemPhieuService _quyenXemPhieuService;
        private readonly ITieuChiRepository _tieuChiRepository;
        private readonly ISoHieuService _soHieuService;
        private readonly ITepDinhKemService _tepDinhKemService;
        private readonly IChuKyPhieuService _chuKyPhieuService;
        private readonly IChuKyPhieuRepository _chuKyPhieuRepository;
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly IUnitOfWork _unitOfWork;

        public Phieu1Service(
            IPhieu1KiemTraRepository phieu1Repository,
            IPhieu1ChiTietRepository phieu1ChiTietRepository,
            IPhieu1KetLuanRepository phieu1KetLuanRepository,
            IBepAnRepository bepAnRepository,
            IPhongBanRepository phongBanRepository,
            INhaThauRepository nhaThauRepository,
            INguoiDungPhieuQuyenRepository nguoiDungPhieuQuyenRepository,
            IQuyenXemPhieuService quyenXemPhieuService,
            ITieuChiRepository tieuChiRepository,
            ISoHieuService soHieuService,
            ITepDinhKemService tepDinhKemService,
            IChuKyPhieuService chuKyPhieuService,
            IChuKyPhieuRepository chuKyPhieuRepository,
            INguoiDungRepository nguoiDungRepository,
            IUnitOfWork unitOfWork)
        {
            _phieu1Repository = phieu1Repository;
            _phieu1ChiTietRepository = phieu1ChiTietRepository;
            _phieu1KetLuanRepository = phieu1KetLuanRepository;
            _bepAnRepository = bepAnRepository;
            _phongBanRepository = phongBanRepository;
            _nhaThauRepository = nhaThauRepository;
            _nguoiDungPhieuQuyenRepository = nguoiDungPhieuQuyenRepository;
            _quyenXemPhieuService = quyenXemPhieuService;
            _tieuChiRepository = tieuChiRepository;
            _soHieuService = soHieuService;
            _tepDinhKemService = tepDinhKemService;
            _chuKyPhieuService = chuKyPhieuService;
            _chuKyPhieuRepository = chuKyPhieuRepository;
            _nguoiDungRepository = nguoiDungRepository;
            _unitOfWork = unitOfWork;
        }

        // Snapshot tên tiêu chí tại thời điểm lưu — tra 1 lần theo batch, tránh
        // N+1. Dòng tự thêm (TieuChiId = NULL) không cần snapshot (đã có sẵn
        // NoiDungTuThem do người dùng gõ tay).
        private async Task<Dictionary<int, string>> LayTenTieuChiTheoBatchAsync(IEnumerable<Phieu1ChiTietRequest> chiTiet)
        {
            var tieuChiIds = chiTiet.Where(d => d.TieuChiId.HasValue).Select(d => d.TieuChiId!.Value).Distinct().ToList();
            if (tieuChiIds.Count == 0) return new Dictionary<int, string>();

            var tieuChi = await _tieuChiRepository.FindAsync(x => tieuChiIds.Contains(x.Id));
            return tieuChi.ToDictionary(x => x.Id, x => x.NoiDung);
        }

        // Quyền "Đánh giá / nhập liệu" theo NguoiDungPhieuQuyen (xem
        // 02. Phantich/modules/VaiTro.md mục 9) — KHÔNG hard-code theo phòng
        // ban: cả P.ĐN và P.ATMT đều có thể được cấp (giữ đúng thiết kế gốc
        // "2 phòng ban lập Phiếu 1 độc lập để đối chiếu chéo", xem
        // Phieu1_KiemTraVSATTP.md).
        // laAdmin bypass, giống hệt CoQuyen() ở Program.cs / coQuyen() ở FE
        // (quyenV2.ts) — admin (VaiTro.LaQuanTriVien) luôn được phép mọi thao
        // tác, không bị chặn bởi NguoiDungPhieuQuyen.
        private async Task KiemTraQuyenDanhGiaAsync(int? nguoiDungId, bool laAdmin)
        {
            if (laAdmin) return;

            if (!nguoiDungId.HasValue || !await _nguoiDungPhieuQuyenRepository.AnyAsync(
                    x => x.NguoiDungId == nguoiDungId.Value && x.LoaiPhieu == "PHIEU1" && x.DuocDanhGia))
                throw new ApiException(
                    "Bạn không có quyền tạo Phiếu 1 — liên hệ Admin để được phân quyền \"Đánh giá / nhập liệu\" ở mục Phân quyền theo Phiếu",
                    StatusCodes.Status403Forbidden);
        }

        // Quyền XEM (danh sách + chi tiết) của tài khoản NỘI BỘ — nhà thầu có
        // luật xem riêng (ép lọc theo nhaThauCuaNguoiGoi), không đi qua đây.
        // laAdmin bypass giống KiemTraQuyenDanhGiaAsync.
        private async Task KiemTraQuyenXemAsync(int? nguoiDungId, bool laAdmin, int? nhaThauCuaNguoiGoi)
        {
            if (laAdmin || nhaThauCuaNguoiGoi.HasValue) return;

            if (!nguoiDungId.HasValue || !await _quyenXemPhieuService.CoQuyenXemAsync(nguoiDungId.Value, "PHIEU1"))
                throw new ApiException(
                    "Bạn không có quyền truy cập Phiếu 1 — liên hệ Admin để được phân quyền ở mục Phân quyền theo Phiếu",
                    StatusCodes.Status403Forbidden);
        }

        // Resolve tên người đã ký (ĐÃ DUYỆT, lượt ký mới nhất) của từng phiếu
        // trong 1 trang danh sách — batch 1 lần, tránh N+1. Phân biệt "Nhà
        // thầu ký"/"Người (phòng ban) ký" theo NhaThauId của TÀI KHOẢN đã ký
        // (NguoiDung.NhaThauId có giá trị = tài khoản nhà thầu), KHÔNG theo
        // BuocThuTu — đúng với mọi cách Admin cấu hình thứ tự bước ở màn
        // "Luồng ký" (xem LuongTrinhKy.md).
        private async Task GanTenNguoiDaKyAsync(List<Phieu1DanhSachItemDto> items)
        {
            var ids = items.Select(x => x.Id).ToList();
            if (ids.Count == 0) return;

            var banGhi = (await _chuKyPhieuRepository.FindAsync(
                x => x.LoaiDoiTuong == "PHIEU1" && ids.Contains(x.DoiTuongId) && x.TrangThai == "DA_DUYET"))
                .ToList();
            if (banGhi.Count == 0) return;

            var luotKyMoiNhat = banGhi.GroupBy(x => x.DoiTuongId).ToDictionary(g => g.Key, g => g.Max(x => x.LuotKy));
            banGhi = banGhi.Where(x => x.LuotKy == luotKyMoiNhat[x.DoiTuongId]).ToList();

            var nguoiKyIds = banGhi.Where(x => x.NguoiKyId.HasValue).Select(x => x.NguoiKyId!.Value).Distinct().ToList();
            var nguoiDungMap = nguoiKyIds.Count == 0
                ? new Dictionary<int, NguoiDung>()
                : (await _nguoiDungRepository.FindAsync(x => nguoiKyIds.Contains(x.Id))).ToDictionary(x => x.Id);

            var theoPhieu = items.ToDictionary(x => x.Id);
            foreach (var nhom in banGhi.GroupBy(x => x.DoiTuongId))
            {
                if (!theoPhieu.TryGetValue(nhom.Key, out var item)) continue;

                item.TenNhaThauDaKy = nhom
                    .Where(x => x.NguoiKyId.HasValue && (nguoiDungMap.GetValueOrDefault(x.NguoiKyId.Value)?.NhaThauId.HasValue ?? false))
                    .Select(x => nguoiDungMap[x.NguoiKyId!.Value].HoTen)
                    .FirstOrDefault();

                item.TenNguoiDaKy = nhom
                    .Where(x => x.NguoiKyId.HasValue && !(nguoiDungMap.GetValueOrDefault(x.NguoiKyId.Value)?.NhaThauId.HasValue ?? false))
                    .Select(x => nguoiDungMap[x.NguoiKyId!.Value].HoTen)
                    .FirstOrDefault();
            }
        }

        public async Task<PagedResultDto<Phieu1DanhSachItemDto>> DanhSachAsync(
            int? bepAnId, int? phongBanId, int? nhaThauId, string? trangThai, DateTime? tuNgay, DateTime? denNgay,
            string? tuKhoa, bool chiCuaToi, int page, int pageSize,
            int? nhaThauCuaNguoiGoi, int? nguoiDungId, bool laAdmin, int? phongBanCuaNguoiGoi)
        {
            await KiemTraQuyenXemAsync(nguoiDungId, laAdmin, nhaThauCuaNguoiGoi);

            var query = _phieu1Repository.Query();

            // Tài khoản nhà thầu chỉ xem được phiếu của chính mình — ép lọc,
            // bỏ qua nhaThauId truyền vào nếu khác (xem DangNhap.md). Đồng
            // thời KHÔNG được thấy phiếu Nháp/Bị từ chối (chưa gửi ký/không
            // liên quan tới họ ở luồng ký đơn giản hiện tại — xem
            // LuongTrinhKy.md) — ép cứng ở Service để không bị lách qua
            // tham số trangThai truyền lên.
            if (nhaThauCuaNguoiGoi.HasValue)
            {
                query = query.Where(x => x.NhaThauId == nhaThauCuaNguoiGoi
                    && (x.TrangThai == "CHO_KY" || x.TrangThai == "DA_DUYET"));
            }
            else if (nhaThauId.HasValue) query = query.Where(x => x.NhaThauId == nhaThauId);

            // Tài khoản NỘI BỘ (không phải Admin): các phòng ban chỉ đối chiếu
            // chéo được với NHAU khi phiếu đã DA_DUYET; phiếu còn đang
            // NHAP/CHO_KY/TU_CHOI (chưa hoàn tất) chỉ ĐÚNG phòng ban đã lập mới
            // thấy — ép cứng ở Service để không bị lách qua tham số phongBanId/
            // trangThai truyền lên. Admin không bị giới hạn này (bypass, giống
            // KiemTraQuyenXemAsync).
            if (!laAdmin && !nhaThauCuaNguoiGoi.HasValue)
                query = query.Where(x => x.TrangThai == "DA_DUYET" || x.PhongBanId == phongBanCuaNguoiGoi);

            if (bepAnId.HasValue) query = query.Where(x => x.BepAnId == bepAnId);
            if (phongBanId.HasValue) query = query.Where(x => x.PhongBanId == phongBanId);
            if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(x => x.TrangThai == trangThai);
            if (tuNgay.HasValue) query = query.Where(x => x.NgayKiemTra >= tuNgay.Value.Date);
            if (denNgay.HasValue) query = query.Where(x => x.NgayKiemTra <= denNgay.Value.Date);
            if (!string.IsNullOrWhiteSpace(tuKhoa)) query = query.Where(x => x.SoHieu.Contains(tuKhoa));
            if (chiCuaToi && nguoiDungId.HasValue) query = query.Where(x => x.NguoiTao == nguoiDungId.Value);

            query = query.OrderByDescending(x => x.NgayKiemTra).ThenByDescending(x => x.Id);
            var tongSo = query.Count();
            var danhSach = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var items = danhSach.Select(p => new Phieu1DanhSachItemDto
            {
                Id = p.Id,
                SoHieu = p.SoHieu,
                NgayKiemTra = p.NgayKiemTra,
                BepAnId = p.BepAnId,
                NhaThauId = p.NhaThauId,
                PhongBanId = p.PhongBanId,
                NguoiTao = p.NguoiTao,
                TrangThai = p.TrangThai,
                NgayTao = p.NgayTao,
            }).ToList();
            await GanTenNguoiDaKyAsync(items);

            return new PagedResultDto<Phieu1DanhSachItemDto> { Items = items, TotalCount = tongSo, Page = page, PageSize = pageSize };
        }

        public async Task<Phieu1ResponseDto> ChiTietAsync(int id, int? nhaThauCuaNguoiGoi, int? nguoiDungId, bool laAdmin, int? phongBanCuaNguoiGoi)
        {
            await KiemTraQuyenXemAsync(nguoiDungId, laAdmin, nhaThauCuaNguoiGoi);
            return await LayChiTietAsync(id, nhaThauCuaNguoiGoi, laAdmin, phongBanCuaNguoiGoi);
        }

        // Tách khỏi ChiTietAsync để các thao tác NỘI BỘ (Them/Sua/GuiKy/...)
        // tự build lại response DTO sau khi ghi mà KHÔNG phải soi lại quyền
        // XEM — người vừa tạo/sửa/ký phiếu chắc chắn đã qua đúng gate của
        // hành động đó rồi, không cần kiểm tra thêm lần nữa (gọi với
        // laAdmin=true để bỏ qua ràng buộc phòng ban bên dưới).
        private async Task<Phieu1ResponseDto> LayChiTietAsync(int id, int? nhaThauCuaNguoiGoi, bool laAdmin = true, int? phongBanCuaNguoiGoi = null)
        {
            var phieu = await _phieu1Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu kiểm tra", StatusCodes.Status404NotFound);

            if (nhaThauCuaNguoiGoi.HasValue && phieu.NhaThauId != nhaThauCuaNguoiGoi)
                throw new ApiException("Bạn không có quyền xem phiếu của nhà thầu khác", StatusCodes.Status403Forbidden);

            // Cùng luật với DanhSachAsync: tài khoản nội bộ không phải Admin chỉ
            // xem chi tiết phiếu chưa DA_DUYET nếu đúng phòng ban đã lập.
            if (!laAdmin && !nhaThauCuaNguoiGoi.HasValue && phieu.TrangThai != "DA_DUYET" && phieu.PhongBanId != phongBanCuaNguoiGoi)
                throw new ApiException("Bạn không có quyền xem phiếu của phòng ban khác", StatusCodes.Status403Forbidden);

            var chiTiet = await _phieu1ChiTietRepository.FindAsync(x => x.PhieuId == id);
            var ketLuan = await _phieu1KetLuanRepository.FirstOrDefaultAsync(x => x.PhieuId == id);

            return new Phieu1ResponseDto
            {
                Phieu = phieu,
                ChiTiet = chiTiet.OrderBy(x => x.ThuTu).ToList(),
                KetLuan = ketLuan
            };
        }

        public async Task<Phieu1ResponseDto> ThemAsync(Phieu1Request request, int? nguoiTaoId, bool laAdmin)
        {
            await KiemTraQuyenDanhGiaAsync(nguoiTaoId, laAdmin);

            var bepAn = await _bepAnRepository.GetByIdAsync(request.BepAnId)
                ?? throw new ApiException("Không tìm thấy bếp ăn", StatusCodes.Status404NotFound);
            DanhMucHoatDong.KiemTraBepAn(bepAn);

            // Nhà thầu do người lập phiếu tự chọn (KHÔNG suy ra từ BepAn.NhaThauId)
            // — hiện chưa có bảng liên kết đáng tin cậy giữa nhà thầu và
            // bếp ăn/nhà ăn theo thời gian (1 bếp ăn có thể đổi nhà thầu vận
            // hành qua các đợt hợp đồng khác nhau).
            var nhaThau = await _nhaThauRepository.GetByIdAsync(request.NhaThauId)
                ?? throw new ApiException("Không tìm thấy nhà thầu", StatusCodes.Status404NotFound);
            DanhMucHoatDong.KiemTraNhaThau(nhaThau);

            var phongBan = await _phongBanRepository.GetByIdAsync(request.PhongBanId)
                ?? throw new ApiException("Không tìm thấy phòng ban", StatusCodes.Status404NotFound);

            var ngay = request.NgayKiemTra.Date;
            var soHieu = await SinhSoHieuAsync(phongBan.Ma, phongBan.Ma, ngay.Year);

            var phieu = new Phieu1KiemTra
            {
                SoHieu = soHieu,
                NgayKiemTra = ngay,
                BepAnId = request.BepAnId,
                NhaThauId = request.NhaThauId,
                PhongBanId = request.PhongBanId,
                NguoiTao = nguoiTaoId,
                TrangThai = "NHAP",
                NgayTao = DateTime.Now
            };

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _phieu1Repository.AddAsync(phieu);
                await _unitOfWork.SaveChangesAsync(); // cần Id thật của phieu trước khi ghi chi tiết/kết luận

                var tenTieuChiTheoId = await LayTenTieuChiTheoBatchAsync(request.ChiTiet);
                foreach (var dong in request.ChiTiet)
                {
                    await _phieu1ChiTietRepository.AddAsync(new Phieu1ChiTiet
                    {
                        PhieuId = phieu.Id,
                        NhomId = dong.NhomId,
                        TieuChiId = dong.TieuChiId,
                        TenTieuChi = dong.TieuChiId.HasValue && tenTieuChiTheoId.TryGetValue(dong.TieuChiId.Value, out var ten) ? ten : null,
                        NoiDungTuThem = dong.NoiDungTuThem,
                        KetQua = dong.KetQua,
                        GhiChu = dong.GhiChu,
                        ThuTu = dong.ThuTu
                    });
                }
                await _unitOfWork.SaveChangesAsync(); // cần Id thật của từng dòng chi tiết để chốt liên kết CKEditor

                var chiTietDaLuu = await _phieu1ChiTietRepository.FindAsync(x => x.PhieuId == phieu.Id);
                foreach (var dong in chiTietDaLuu)
                    await _tepDinhKemService.ChotLienKetCkeditorAsync(dong.Id, dong.GhiChu);

                var ketLuan = TinhKetLuan(phieu.Id, chiTietDaLuu, request.KetLuanGhiChu);
                await _phieu1KetLuanRepository.AddAsync(ketLuan);
                await _unitOfWork.SaveChangesAsync();
                await _tepDinhKemService.ChotLienKetCkeditorAsync(ketLuan.Id, ketLuan.GhiChu);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return await LayChiTietAsync(phieu.Id, null);
        }

        public async Task<Phieu1ResponseDto> SuaAsync(int id, Phieu1Request request)
        {
            var phieu = await _phieu1Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu kiểm tra", StatusCodes.Status404NotFound);

            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể sửa phiếu đang ở trạng thái Nháp hoặc bị Từ chối");

            // Chỉ chặn bếp ăn/nhà thầu đã ngừng khi ĐỔI sang bản ghi khác — giữ
            // nguyên giá trị cũ thì vẫn lưu được (xem Common/DanhMucHoatDong.cs).
            if (request.BepAnId != phieu.BepAnId)
            {
                var bepAn = await _bepAnRepository.GetByIdAsync(request.BepAnId)
                    ?? throw new ApiException("Không tìm thấy bếp ăn", StatusCodes.Status404NotFound);
                DanhMucHoatDong.KiemTraBepAn(bepAn);
            }

            var nhaThau = await _nhaThauRepository.GetByIdAsync(request.NhaThauId)
                ?? throw new ApiException("Không tìm thấy nhà thầu", StatusCodes.Status404NotFound);
            if (request.NhaThauId != phieu.NhaThauId)
                DanhMucHoatDong.KiemTraNhaThau(nhaThau);

            var ngay = request.NgayKiemTra.Date;

            phieu.NgayKiemTra = ngay;
            phieu.BepAnId = request.BepAnId;
            phieu.NhaThauId = request.NhaThauId;
            phieu.PhongBanId = request.PhongBanId;
            phieu.TrangThai = "NHAP"; // sửa phiếu bị từ chối -> quay lại Nháp, cần gửi ký lại từ đầu
            _phieu1Repository.Update(phieu);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var hienTai = await _phieu1ChiTietRepository.FindAsync(x => x.PhieuId == id);
                var idGiuLai = request.ChiTiet.Where(c => c.Id.HasValue && c.Id > 0).Select(c => c.Id!.Value).ToHashSet();

                var canXoa = hienTai.Where(x => !idGiuLai.Contains(x.Id)).ToList();
                _phieu1ChiTietRepository.RemoveRange(canXoa);

                // Snapshot lại tên tiêu chí mỗi lần lưu — phiếu chỉ sửa được khi
                // còn NHAP/TU_CHOI (chặn ở đầu hàm), nên snapshot chỉ "chốt cứng"
                // đúng lúc phiếu chuyển CHO_KY (gửi ký), không còn SuaAsync nào
                // gọi được nữa sau đó — xem Phieu1ChiTiet.TenTieuChi.
                var tenTieuChiTheoId = await LayTenTieuChiTheoBatchAsync(request.ChiTiet);
                foreach (var dong in request.ChiTiet)
                {
                    var tenTieuChiMoi = dong.TieuChiId.HasValue && tenTieuChiTheoId.TryGetValue(dong.TieuChiId.Value, out var ten)
                        ? ten
                        : null;

                    if (dong.Id.HasValue && dong.Id > 0)
                    {
                        var dongHienTai = hienTai.FirstOrDefault(x => x.Id == dong.Id);
                        if (dongHienTai == null) continue;

                        dongHienTai.NhomId = dong.NhomId;
                        dongHienTai.TieuChiId = dong.TieuChiId;
                        dongHienTai.TenTieuChi = tenTieuChiMoi;
                        dongHienTai.NoiDungTuThem = dong.NoiDungTuThem;
                        dongHienTai.KetQua = dong.KetQua;
                        dongHienTai.GhiChu = dong.GhiChu;
                        dongHienTai.ThuTu = dong.ThuTu;
                        _phieu1ChiTietRepository.Update(dongHienTai);
                    }
                    else
                    {
                        await _phieu1ChiTietRepository.AddAsync(new Phieu1ChiTiet
                        {
                            PhieuId = id,
                            NhomId = dong.NhomId,
                            TieuChiId = dong.TieuChiId,
                            TenTieuChi = tenTieuChiMoi,
                            NoiDungTuThem = dong.NoiDungTuThem,
                            KetQua = dong.KetQua,
                            GhiChu = dong.GhiChu,
                            ThuTu = dong.ThuTu
                        });
                    }
                }
                await _unitOfWork.SaveChangesAsync();

                var chiTietDaLuu = await _phieu1ChiTietRepository.FindAsync(x => x.PhieuId == id);
                foreach (var dong in chiTietDaLuu)
                    await _tepDinhKemService.ChotLienKetCkeditorAsync(dong.Id, dong.GhiChu);

                var ketLuanHienTai = await _phieu1KetLuanRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
                var ketLuanMoi = TinhKetLuan(id, chiTietDaLuu, request.KetLuanGhiChu);

                if (ketLuanHienTai == null)
                {
                    await _phieu1KetLuanRepository.AddAsync(ketLuanMoi);
                }
                else
                {
                    ketLuanHienTai.SoLuongDat = ketLuanMoi.SoLuongDat;
                    ketLuanHienTai.TongSoTieuChi = ketLuanMoi.TongSoTieuChi;
                    ketLuanHienTai.TyLePhanTram = ketLuanMoi.TyLePhanTram;
                    ketLuanHienTai.KetLuan = ketLuanMoi.KetLuan;
                    ketLuanHienTai.DiemDanhGia = ketLuanMoi.DiemDanhGia;
                    ketLuanHienTai.GhiChu = ketLuanMoi.GhiChu;
                    _phieu1KetLuanRepository.Update(ketLuanHienTai);
                }
                await _unitOfWork.SaveChangesAsync();

                var ketLuanSauLuu = ketLuanHienTai ?? ketLuanMoi;
                await _tepDinhKemService.ChotLienKetCkeditorAsync(ketLuanSauLuu.Id, ketLuanSauLuu.GhiChu);

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return await LayChiTietAsync(id, null);
        }

        // laAdmin bypass ràng buộc trạng thái — Admin xóa được phiếu ở BẤT KỲ
        // trạng thái nào (VD đã gửi ký/đã duyệt) từ trang danh sách, khác hẳn
        // người dùng thường chỉ xóa được phiếu Nháp. Kèm dọn dẹp ChuKyPhieu
        // (dữ liệu luồng ký — nằm NGOÀI các bảng của riêng Phiếu 1, xem
        // LuongTrinhKy.md) vì phiếu không NHAP có thể đã có các dòng này.
        public async Task XoaAsync(int id, bool laAdmin)
        {
            var phieu = await _phieu1Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu kiểm tra", StatusCodes.Status404NotFound);

            if (!laAdmin && phieu.TrangThai != "NHAP")
                throw new ApiException("Chỉ có thể xóa phiếu đang ở trạng thái Nháp");

            var chiTiet = await _phieu1ChiTietRepository.FindAsync(x => x.PhieuId == id);
            _phieu1ChiTietRepository.RemoveRange(chiTiet);

            var ketLuan = await _phieu1KetLuanRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            if (ketLuan != null)
                _phieu1KetLuanRepository.Remove(ketLuan);

            var chuKy = await _chuKyPhieuRepository.FindAsync(x => x.LoaiDoiTuong == "PHIEU1" && x.DoiTuongId == id);
            _chuKyPhieuRepository.RemoveRange(chuKy);

            _phieu1Repository.Remove(phieu);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<Phieu1KiemTra> GuiKyAsync(int id)
        {
            var phieu = await _phieu1Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu kiểm tra", StatusCodes.Status404NotFound);

            if (phieu.TrangThai != "NHAP")
                throw new ApiException("Chỉ có thể gửi ký khi phiếu đang ở trạng thái Nháp");

            phieu.TrangThai = "CHO_KY";
            _phieu1Repository.Update(phieu);
            await _unitOfWork.SaveChangesAsync();

            // Quay lại luồng ký ĐẦY ĐỦ theo MauLuongKy (giống Phiếu 3/4) —
            // KHÔNG còn auto-duyệt "Người đánh giá" nữa. Nhà thầu ký trước
            // (BuocThuTu nhỏ hơn), Phòng ban ký sau — thứ tự do Admin cấu
            // hình ở màn "Luồng ký" (xem VaiTro.md/LuongTrinhKy.md).
            await _chuKyPhieuService.KhoiTaoLuongKyAsync("PHIEU1", id);
            return phieu;
        }

        public async Task<Phieu1KiemTra> DongBoTrangThaiAsync(int id)
        {
            var phieu = await _phieu1Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu kiểm tra", StatusCodes.Status404NotFound);

            var trangThaiKy = await _chuKyPhieuService.TrangThaiTongAsync("PHIEU1", id);
            if (trangThaiKy is "DA_DUYET" or "TU_CHOI" or "CHO_KY")
            {
                phieu.TrangThai = trangThaiKy;
                _phieu1Repository.Update(phieu);
                await _unitOfWork.SaveChangesAsync();
            }

            return phieu;
        }

        // Quy ước đánh số Phiếu 1 (Phiếu kiểm tra VSATTP): {seq:003}/{năm}/
        // PKTCTVSATTP-{tên phòng ban} — tên phòng ban lấy theo phòng ban của
        // NGƯỜI LẬP phiếu (request.PhongBanId, tự gán = phòng ban của user
        // đang đăng nhập lúc tạo, xem Phieu1FormPage.tsx). 1 dãy số RIÊNG cho
        // mỗi phòng ban (khóa đếm vẫn dùng Ma, không đổi khi đổi tên phòng
        // ban), bắt đầu từ 001 ngày 1/1 và reset lại vào ngày 1/1 năm sau —
        // cùng cách đánh số theo năm với Phiếu 2, xem
        // Phieu2Service.SinhSoHieuAsync. Tách riêng theo phòng ban vì Phiếu 3
        // liệt kê "Căn cứ đánh giá" theo TỪNG phòng ban (xem
        // Phieu3FormPage.tsx/canCuPhieu1) — số hiệu phải liền mạch trong phạm
        // vi 1 phòng ban để danh sách đó có ý nghĩa.
        private async Task<string> SinhSoHieuAsync(string maPhongBan, string tenPhongBan, int nam)
        {
            var seq = await _soHieuService.SinhSoTiepTheoAsync("PHIEU1", maPhongBan, nam, null);
            return $"{seq:D3}/{nam}/PKTCTVSATTP-{tenPhongBan}";
        }

        private static Phieu1KetLuan TinhKetLuan(int phieuId, List<Phieu1ChiTiet> chiTiet, string? ghiChu)
        {
            var hopLe = chiTiet.Where(c => c.KetQua == "DAT" || c.KetQua == "KHONG_DAT").ToList();
            var tongSo = hopLe.Count;
            var soDat = hopLe.Count(c => c.KetQua == "DAT");
            decimal? tyLe = tongSo > 0 ? Math.Round((decimal)soDat / tongSo * 100, 2) : null;
            var ketLuan = tyLe.HasValue ? (tyLe.Value >= 50 ? "DAT" : "KHONG_DAT") : null;
            decimal? diem = tyLe.HasValue ? Math.Round(tyLe.Value * 5 / 100, 2) : null;

            return new Phieu1KetLuan
            {
                PhieuId = phieuId,
                SoLuongDat = tongSo > 0 ? soDat : null,
                TongSoTieuChi = tongSo > 0 ? tongSo : null,
                TyLePhanTram = tyLe,
                KetLuan = ketLuan,
                DiemDanhGia = diem,
                GhiChu = ghiChu
            };
        }
    }
}
