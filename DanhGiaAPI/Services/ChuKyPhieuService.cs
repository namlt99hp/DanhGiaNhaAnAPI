using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.LuongKy;
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
        private readonly INguoiDungMauLuongKyRepository _nguoiDungMauLuongKyRepository;
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly IPhieuNhaThauResolver _phieuNhaThauResolver;
        private readonly IUnitOfWork _unitOfWork;

        public ChuKyPhieuService(
            IChuKyPhieuRepository chuKyPhieuRepository,
            IMauLuongKyRepository mauLuongKyRepository,
            INguoiDungMauLuongKyRepository nguoiDungMauLuongKyRepository,
            INguoiDungRepository nguoiDungRepository,
            IPhieuNhaThauResolver phieuNhaThauResolver,
            IUnitOfWork unitOfWork)
        {
            _chuKyPhieuRepository = chuKyPhieuRepository;
            _mauLuongKyRepository = mauLuongKyRepository;
            _nguoiDungMauLuongKyRepository = nguoiDungMauLuongKyRepository;
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

        public async Task<ChuKyPhieu> KyAsync(int id, int nguoiKyId, int? chuKyId, string? ghiChu, int? nguoiKyThayId)
        {
            var buoc = await LayBuocDangChoKyAsync(id);

            await KiemTraTuanTuAsync(buoc);
            // Người thao tác (đang đăng nhập) LUÔN phải tự đủ điều kiện ký bước
            // này, kể cả khi đang ký thay cho người khác — tránh người ngoài
            // nhóm (không cùng nhà thầu/phòng ban/vai trò) tự ý gán chữ ký cho ai đó.
            await KiemTraQuyenKyAsync(buoc, nguoiKyId);

            var nguoiKyThucTe = nguoiKyThayId ?? nguoiKyId;
            if (nguoiKyThayId.HasValue && nguoiKyThayId != nguoiKyId)
                await KiemTraQuyenKyAsync(buoc, nguoiKyThayId.Value);

            KiemTraNguoiKyDuKien(buoc, nguoiKyThucTe);

            buoc.TrangThai = "DA_DUYET";
            buoc.NguoiKyId = nguoiKyThucTe;
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
            KiemTraNguoiKyDuKien(buoc, nguoiKyId);

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

        // Kiểm tra quyền ký — xem modules/VaiTro.md mục 9 (mô hình "phân quyền
        // theo Phiếu", thay hoàn toàn cơ chế LoaiNguoiKy = VAI_TRO cũ).
        //
        // Ràng buộc CHUNG cho cả 3 loại: người ký phải được Admin gán TRỰC
        // TIẾP vào đúng dòng MauLuongKy này (NguoiDungMauLuongKy) — không còn
        // "cùng phòng ban/nhà thầu/vai trò là tự động ký được" nữa. Với
        // PHONG_BAN/NHA_THAU, đây là lớp kiểm soát THỨ 2 cộng thêm vào việc
        // khớp phòng ban/nhà thầu (cả 2 điều kiện đều phải đúng).
        //
        // (Sửa 2026-09-02) Có thể có NHIỀU dòng MauLuongKy cùng
        // (LoaiPhieu, BuocThuTu) khi ký SONG SONG nhiều phòng ban (VD Phiếu 4
        // bước 1: 1 dòng PHONG_BAN=PDN + 1 dòng PHONG_BAN=PATMT) — 1 dòng
        // ChuKyPhieu không "thuộc về" cố định 1 dòng MauLuongKy nào (chỉ là 1
        // trong N chữ ký cần cho bước đó), nên KHÔNG được lấy FirstOrDefault
        // (luôn trúng đúng 1 dòng cố định, làm người được gán ở dòng còn lại
        // luôn bị từ chối 403 dù hợp lệ) — phải duyệt qua TẤT CẢ các dòng cùng
        // bước, người ký hợp lệ nếu thỏa ĐIỀU KIỆN CỦA ÍT NHẤT 1 dòng.
        private async Task KiemTraQuyenKyAsync(ChuKyPhieu buoc, int nguoiKyId)
        {
            var cacMauBuoc = _mauLuongKyRepository.Query()
                .Where(x => x.LoaiPhieu == buoc.LoaiDoiTuong && x.BuocThuTu == buoc.BuocThuTu)
                .ToList();

            if (cacMauBuoc.Count == 0)
                return;

            foreach (var mauBuoc in cacMauBuoc)
            {
                if (await ThoaMauBuocAsync(buoc, mauBuoc, nguoiKyId))
                    return;
            }

            throw new ApiException("Bạn chưa được cấp quyền ký bước này", StatusCodes.Status403Forbidden);
        }

        // 1 dòng MauLuongKy cụ thể — trả về true nếu nguoiKyId thỏa ĐỦ cả 2 lớp:
        // (1) được Admin gán trực tiếp vào đúng dòng này, VÀ (2) khớp điều kiện
        // riêng theo LoaiNguoiKy (PHONG_BAN/NHA_THAU/TRUC_TIEP).
        private async Task<bool> ThoaMauBuocAsync(ChuKyPhieu buoc, MauLuongKy mauBuoc, int nguoiKyId)
        {
            var duocGanTrucTiep = await _nguoiDungMauLuongKyRepository.AnyAsync(
                x => x.NguoiDungId == nguoiKyId && x.MauLuongKyId == mauBuoc.Id);
            if (!duocGanTrucTiep)
                return false;

            switch (mauBuoc.LoaiNguoiKy)
            {
                case "PHONG_BAN":
                    if (!mauBuoc.PhongBanId.HasValue)
                        return false;

                    var nguoiDungPhongBan = await _nguoiDungRepository.GetByIdAsync(nguoiKyId);
                    return nguoiDungPhongBan?.PhongBanId == mauBuoc.PhongBanId;

                case "NHA_THAU":
                    var nhaThauIdCuaPhieu = await _phieuNhaThauResolver.LayNhaThauIdAsync(buoc.LoaiDoiTuong, buoc.DoiTuongId);
                    if (!nhaThauIdCuaPhieu.HasValue)
                        return false;

                    var nguoiDungNhaThau = await _nguoiDungRepository.GetByIdAsync(nguoiKyId);
                    return nguoiDungNhaThau?.NhaThauId == nhaThauIdCuaPhieu;

                default: // TRUC_TIEP — đã đủ điều kiện qua "duocGanTrucTiep" ở trên
                    return true;
            }
        }

        // Ràng buộc cứng khi bước đã có NguoiKyDuKienId: chữ ký cuối cùng BẮT
        // BUỘC phải đứng tên đúng người được chỉ định — chặn cả "tự ký" (người
        // khác tự đăng nhập ký) lẫn "ký thay ai đó khác người đã chỉ định".
        private static void KiemTraNguoiKyDuKien(ChuKyPhieu buoc, int nguoiKyThucTe)
        {
            if (buoc.NguoiKyDuKienId.HasValue && buoc.NguoiKyDuKienId != nguoiKyThucTe)
                throw new ApiException("Bước này đã chỉ định người ký khác, bạn không thể ký/từ chối thay", StatusCodes.Status403Forbidden);
        }

        public async Task<ChuKyPhieu> DatNguoiKyDuKienAsync(int id, int nguoiThucHienId, int nguoiKyDuKienId)
        {
            var buoc = await LayBuocDangChoKyAsync(id);

            // Cả người đặt lẫn người được chỉ định đều phải tự đủ điều kiện ký
            // đúng bước này (xem VaiTro.md/LuongTrinhKy.md) — tránh người ngoài
            // nhóm tự ý áp đặt ai sẽ là người ký của nhóm khác.
            await KiemTraQuyenKyAsync(buoc, nguoiThucHienId);
            await KiemTraQuyenKyAsync(buoc, nguoiKyDuKienId);

            buoc.NguoiKyDuKienId = nguoiKyDuKienId;
            _chuKyPhieuRepository.Update(buoc);
            await _unitOfWork.SaveChangesAsync();
            return buoc;
        }

        // Danh sách người đủ điều kiện ký 1 bước — dùng cho dropdown FE "chỉ
        // định người ký" / "ký thay". Cùng logic điều kiện với KiemTraQuyenKyAsync,
        // chỉ khác là liệt kê thay vì kiểm tra 1 người.
        //
        // (Sửa 2026-09-02) Gộp (union) danh sách hợp lệ từ TẤT CẢ các dòng
        // MauLuongKy cùng (LoaiPhieu, BuocThuTu) — cùng lý do với
        // KiemTraQuyenKyAsync: bước ký song song nhiều phòng ban có nhiều dòng
        // cùng BuocThuTu, FirstOrDefault trước đây chỉ liệt kê được người của
        // 1 trong các dòng đó.
        public async Task<List<NguoiKyKhaDungDto>> DanhSachNguoiKyKhaDungAsync(int id)
        {
            var buoc = await _chuKyPhieuRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy bước ký", StatusCodes.Status404NotFound);

            var cacMauBuoc = _mauLuongKyRepository.Query()
                .Where(x => x.LoaiPhieu == buoc.LoaiDoiTuong && x.BuocThuTu == buoc.BuocThuTu)
                .ToList();
            if (cacMauBuoc.Count == 0)
                return new List<NguoiKyKhaDungDto>();

            var ketQua = new List<NguoiDung>();
            foreach (var mauBuoc in cacMauBuoc)
            {
                IQueryable<NguoiDung> query = _nguoiDungRepository.Query();
                switch (mauBuoc.LoaiNguoiKy)
                {
                    case "PHONG_BAN":
                        if (!mauBuoc.PhongBanId.HasValue)
                            continue;

                        query = query.Where(x => x.PhongBanId == mauBuoc.PhongBanId);
                        break;

                    case "NHA_THAU":
                        var nhaThauId = await _phieuNhaThauResolver.LayNhaThauIdAsync(buoc.LoaiDoiTuong, buoc.DoiTuongId);
                        if (!nhaThauId.HasValue)
                            continue;

                        query = query.Where(x => x.NhaThauId == nhaThauId);
                        break;

                    case "TRUC_TIEP":
                        break; // không lọc cấu trúc gì thêm — chỉ dựa vào gán trực tiếp bên dưới

                    default:
                        continue;
                }

                // Giao với danh sách được Admin gán trực tiếp vào đúng dòng này
                // (thay cho lọc theo VaiTro cũ) — xem KiemTraQuyenKyAsync.
                var idDuocGan = (await _nguoiDungMauLuongKyRepository.GetByMauLuongKyIdAsync(mauBuoc.Id))
                    .Select(x => x.NguoiDungId)
                    .ToList();

                ketQua.AddRange(query.Where(x => idDuocGan.Contains(x.Id) && x.TrangThai == "HOAT_DONG"));
            }

            return ketQua
                .DistinctBy(x => x.Id)
                .OrderBy(x => x.HoTen)
                .Select(x => new NguoiKyKhaDungDto { Id = x.Id, HoTen = x.HoTen, TenDangNhap = x.TenDangNhap })
                .ToList();
        }
    }
}
