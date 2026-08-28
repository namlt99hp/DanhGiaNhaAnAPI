using DanhGiaAPI.Common;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    // Service ký dùng chung cho Phiếu 1-4 (đa hình theo LoaiDoiTuong/DoiTuongId).
    // Xem quyết định thiết kế + giả định nghiệp vụ (ký tuần tự, xử lý khi từ chối,
    // giới hạn hiện tại của việc kiểm tra quyền PHONG_BAN/NHA_THAU) trong
    // 02. Phantich/modules/LuongTrinhKy.md.
    public class ChuKyPhieuService : IChuKyPhieuService
    {
        private readonly IChuKyPhieuRepository _chuKyPhieuRepository;
        private readonly IMauLuongKyRepository _mauLuongKyRepository;
        private readonly INguoiDungVaiTroRepository _nguoiDungVaiTroRepository;
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly IPhieuNhaThauResolver _phieuNhaThauResolver;
        private readonly IUnitOfWork _unitOfWork;

        public ChuKyPhieuService(
            IChuKyPhieuRepository chuKyPhieuRepository,
            IMauLuongKyRepository mauLuongKyRepository,
            INguoiDungVaiTroRepository nguoiDungVaiTroRepository,
            INguoiDungRepository nguoiDungRepository,
            IPhieuNhaThauResolver phieuNhaThauResolver,
            IUnitOfWork unitOfWork)
        {
            _chuKyPhieuRepository = chuKyPhieuRepository;
            _mauLuongKyRepository = mauLuongKyRepository;
            _nguoiDungVaiTroRepository = nguoiDungVaiTroRepository;
            _nguoiDungRepository = nguoiDungRepository;
            _phieuNhaThauResolver = phieuNhaThauResolver;
            _unitOfWork = unitOfWork;
        }

        public async Task KhoiTaoLuongKyAsync(string loaiPhieu, int doiTuongId)
        {
            var cacBuoc = _mauLuongKyRepository.Query()
                .Where(x => x.LoaiPhieu == loaiPhieu)
                .OrderBy(x => x.BuocThuTu)
                .ToList();

            if (cacBuoc.Count == 0)
                return;

            // Mỗi lần khởi tạo (kể cả gửi ký lại sau khi bị từ chối) là 1 LƯỢT KÝ
            // mới — dùng để phân biệt với "nhiều người ký song song cùng 1
            // BuocThuTu" (VD Phiếu 4: P.ĐN + P.ATMT cùng ký bước 1, xem MauLuongKy
            // có 2 dòng cùng BuocThuTu). Không dùng nhóm-theo-BuocThuTu nữa vì nó
            // vô tình "nuốt mất" 1 trong 2 người ký song song đó.
            var luotKyTiepTheo = 1 + (_chuKyPhieuRepository.Query()
                .Where(x => x.LoaiDoiTuong == loaiPhieu && x.DoiTuongId == doiTuongId)
                .Select(x => (int?)x.LuotKy)
                .Max() ?? 0);

            var danhSach = cacBuoc.Select(b => new ChuKyPhieu
            {
                LoaiDoiTuong = loaiPhieu,
                DoiTuongId = doiTuongId,
                BuocThuTu = b.BuocThuTu,
                TenBuoc = b.TenBuoc,
                TrangThai = "CHO_KY",
                LuotKy = luotKyTiepTheo,
            });

            await _chuKyPhieuRepository.AddRangeAsync(danhSach);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<ChuKyPhieu>> TienDoKyAsync(string loaiPhieu, int doiTuongId)
        {
            var banGhiMoiNhat = await LayBanGhiMoiNhatAsync(loaiPhieu, doiTuongId);
            return banGhiMoiNhat.OrderBy(x => x.BuocThuTu).ToList();
        }

        public async Task<string> TrangThaiTongAsync(string loaiPhieu, int doiTuongId)
        {
            var tienDo = await LayBanGhiMoiNhatAsync(loaiPhieu, doiTuongId);
            if (tienDo.Count == 0)
                return "CHUA_KHOI_TAO";

            if (tienDo.Any(x => x.TrangThai == "TU_CHOI"))
                return "TU_CHOI";

            var cacBuocBatBuoc = _mauLuongKyRepository.Query()
                .Where(x => x.LoaiPhieu == loaiPhieu && x.BatBuoc)
                .Select(x => x.BuocThuTu)
                .ToHashSet();

            var daHoanTatHetBuocBatBuoc = tienDo
                .Where(x => cacBuocBatBuoc.Contains(x.BuocThuTu))
                .All(x => x.TrangThai == "DA_DUYET");

            return daHoanTatHetBuocBatBuoc ? "DA_DUYET" : "CHO_KY";
        }

        public async Task<ChuKyPhieu> KyAsync(int id, int nguoiKyId, int? chuKyId, string? ghiChu)
        {
            var buoc = await LayBuocDangChoKyAsync(id);

            await KiemTraTuanTuAsync(buoc);
            await KiemTraQuyenKyAsync(buoc, nguoiKyId);

            buoc.TrangThai = "DA_DUYET";
            buoc.NguoiKyId = nguoiKyId;
            buoc.ChuKyId = chuKyId;
            buoc.GhiChu = ghiChu;
            buoc.NgayKy = DateTime.Now;
            _chuKyPhieuRepository.Update(buoc);
            await _unitOfWork.SaveChangesAsync();
            return buoc;
        }

        public async Task<ChuKyPhieu> TuChoiAsync(int id, int nguoiKyId, string ghiChu)
        {
            var buoc = await LayBuocDangChoKyAsync(id);

            await KiemTraQuyenKyAsync(buoc, nguoiKyId);

            buoc.TrangThai = "TU_CHOI";
            buoc.NguoiKyId = nguoiKyId;
            buoc.GhiChu = ghiChu;
            buoc.NgayKy = DateTime.Now;
            _chuKyPhieuRepository.Update(buoc);

            // Luồng ký dừng lại ở đây — hủy các bước CHO_KY còn lại phía sau, KỂ CẢ
            // người ký song song cùng bước (BuocThuTu bằng nhau, VD Phiếu 4: P.ĐN
            // từ chối thì hủy luôn dòng CHO_KY của P.ATMT cùng bước 1) — giả định
            // "từ chối = dừng toàn luồng", xem LuongTrinhKy.md. Chỉ hủy trong CÙNG
            // lượt ký (LuotKy) — không đụng tới các lượt cũ (lịch sử/audit). Khi
            // phiếu được sửa và gửi ký lại, Service của Phiếu phải gọi
            // KhoiTaoLuongKyAsync để tạo 1 lượt ChuKyPhieu mới.
            var cacBuocSau = _chuKyPhieuRepository.Query()
                .Where(x => x.LoaiDoiTuong == buoc.LoaiDoiTuong && x.DoiTuongId == buoc.DoiTuongId)
                .Where(x => x.LuotKy == buoc.LuotKy && x.Id != buoc.Id
                         && x.BuocThuTu >= buoc.BuocThuTu && x.TrangThai == "CHO_KY")
                .ToList();
            _chuKyPhieuRepository.RemoveRange(cacBuocSau);

            await _unitOfWork.SaveChangesAsync();
            return buoc;
        }

        // Lấy TOÀN BỘ dòng của LƯỢT KÝ mới nhất (LuotKy lớn nhất) — hỗ trợ cả 2
        // trường hợp: (a) phiếu bị từ chối rồi khởi tạo lại (lượt cũ có LuotKy nhỏ
        // hơn, bị bỏ qua toàn bộ), và (b) nhiều người ký song song cùng 1
        // BuocThuTu trong CÙNG 1 lượt (giữ lại tất cả, không chỉ 1 dòng).
        private async Task<List<ChuKyPhieu>> LayBanGhiMoiNhatAsync(string loaiPhieu, int doiTuongId)
        {
            var tatCa = _chuKyPhieuRepository.Query()
                .Where(x => x.LoaiDoiTuong == loaiPhieu && x.DoiTuongId == doiTuongId)
                .ToList();

            if (tatCa.Count == 0) return tatCa;

            var luotMoiNhat = tatCa.Max(x => x.LuotKy);
            return tatCa.Where(x => x.LuotKy == luotMoiNhat).ToList();
        }

        private async Task<ChuKyPhieu> LayBuocDangChoKyAsync(int id)
        {
            var buoc = await _chuKyPhieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy bước ký", StatusCodes.Status404NotFound);

            if (buoc.TrangThai != "CHO_KY")
                throw new ApiException("Bước này đã được xử lý (không còn ở trạng thái chờ ký)");

            return buoc;
        }

        // Giả định nghiệp vụ: ký TUẦN TỰ theo BuocThuTu — chỉ chặn bởi các bước
        // BẮT BUỘC (BatBuoc = 1) đứng trước chưa DA_DUYET. Xem LuongTrinhKy.md.
        private async Task KiemTraTuanTuAsync(ChuKyPhieu buoc)
        {
            var banGhiMoiNhat = await LayBanGhiMoiNhatAsync(buoc.LoaiDoiTuong, buoc.DoiTuongId);
            var cacBuocBatBuoc = _mauLuongKyRepository.Query()
                .Where(x => x.LoaiPhieu == buoc.LoaiDoiTuong && x.BatBuoc)
                .Select(x => x.BuocThuTu)
                .ToHashSet();

            var conBuocTruocChuaXong = banGhiMoiNhat.Any(x =>
                x.BuocThuTu < buoc.BuocThuTu &&
                cacBuocBatBuoc.Contains(x.BuocThuTu) &&
                x.TrangThai != "DA_DUYET");

            if (conBuocTruocChuaXong)
                throw new ApiException("Phải hoàn tất (các) bước ký trước đó trước khi ký bước này");
        }

        // Kiểm tra quyền ký theo đúng LoaiNguoiKy cấu hình ở MauLuongKy — xem
        // modules/VaiTro.md mục 7 (phân tích + thiết kế trước khi hoàn thiện
        // 2 nhánh PHONG_BAN/NHA_THAU vốn trước đây bỏ trống).
        private async Task KiemTraQuyenKyAsync(ChuKyPhieu buoc, int nguoiKyId)
        {
            var mauBuoc = _mauLuongKyRepository.Query()
                .FirstOrDefault(x => x.LoaiPhieu == buoc.LoaiDoiTuong && x.BuocThuTu == buoc.BuocThuTu);

            if (mauBuoc == null)
                return;

            switch (mauBuoc.LoaiNguoiKy)
            {
                case "VAI_TRO":
                    if (!mauBuoc.VaiTroId.HasValue)
                        return;

                    var vaiTroCuaNguoiDung = await _nguoiDungVaiTroRepository.GetVaiTroCuaNguoiDungAsync(nguoiKyId);
                    if (!vaiTroCuaNguoiDung.Any(vt => vt.Id == mauBuoc.VaiTroId))
                        throw new ApiException("Bạn không có vai trò được cấu hình để ký bước này", StatusCodes.Status403Forbidden);
                    return;

                case "PHONG_BAN":
                    if (!mauBuoc.PhongBanId.HasValue)
                        return;

                    var nguoiDungPhongBan = await _nguoiDungRepository.GetByIdAsync(nguoiKyId);
                    if (nguoiDungPhongBan?.PhongBanId != mauBuoc.PhongBanId)
                        throw new ApiException("Bạn không thuộc phòng ban được cấu hình để ký bước này", StatusCodes.Status403Forbidden);
                    return;

                case "NHA_THAU":
                    var nhaThauIdCuaPhieu = await _phieuNhaThauResolver.LayNhaThauIdAsync(buoc.LoaiDoiTuong, buoc.DoiTuongId);
                    if (!nhaThauIdCuaPhieu.HasValue)
                        throw new ApiException("Không xác định được nhà thầu của phiếu này để kiểm tra quyền ký", StatusCodes.Status403Forbidden);

                    var nguoiDungNhaThau = await _nguoiDungRepository.GetByIdAsync(nguoiKyId);
                    if (nguoiDungNhaThau?.NhaThauId != nhaThauIdCuaPhieu)
                        throw new ApiException("Bạn không thuộc nhà thầu của phiếu này nên không thể ký bước này", StatusCodes.Status403Forbidden);
                    return;

                default:
                    return;
            }
        }
    }
}
