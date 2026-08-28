using DanhGiaAPI.Common;
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
        private readonly ISoHieuService _soHieuService;
        private readonly ITepDinhKemService _tepDinhKemService;
        private readonly IChuKyPhieuService _chuKyPhieuService;
        private readonly IUnitOfWork _unitOfWork;

        public Phieu1Service(
            IPhieu1KiemTraRepository phieu1Repository,
            IPhieu1ChiTietRepository phieu1ChiTietRepository,
            IPhieu1KetLuanRepository phieu1KetLuanRepository,
            IBepAnRepository bepAnRepository,
            IPhongBanRepository phongBanRepository,
            ISoHieuService soHieuService,
            ITepDinhKemService tepDinhKemService,
            IChuKyPhieuService chuKyPhieuService,
            IUnitOfWork unitOfWork)
        {
            _phieu1Repository = phieu1Repository;
            _phieu1ChiTietRepository = phieu1ChiTietRepository;
            _phieu1KetLuanRepository = phieu1KetLuanRepository;
            _bepAnRepository = bepAnRepository;
            _phongBanRepository = phongBanRepository;
            _soHieuService = soHieuService;
            _tepDinhKemService = tepDinhKemService;
            _chuKyPhieuService = chuKyPhieuService;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Phieu1KiemTra>> DanhSachAsync(int? bepAnId, int? phongBanId, int? nhaThauId, string? trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            var query = _phieu1Repository.Query();

            if (bepAnId.HasValue) query = query.Where(x => x.BepAnId == bepAnId);
            if (phongBanId.HasValue) query = query.Where(x => x.PhongBanId == phongBanId);
            if (nhaThauId.HasValue) query = query.Where(x => x.NhaThauId == nhaThauId);
            if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(x => x.TrangThai == trangThai);
            if (tuNgay.HasValue) query = query.Where(x => x.NgayKiemTra >= tuNgay.Value.Date);
            if (denNgay.HasValue) query = query.Where(x => x.NgayKiemTra <= denNgay.Value.Date);

            return query.OrderByDescending(x => x.NgayKiemTra).ThenByDescending(x => x.Id).ToList();
        }

        public async Task<Phieu1ResponseDto> ChiTietAsync(int id)
        {
            var phieu = await _phieu1Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu kiểm tra", StatusCodes.Status404NotFound);

            var chiTiet = await _phieu1ChiTietRepository.FindAsync(x => x.PhieuId == id);
            var ketLuan = await _phieu1KetLuanRepository.FirstOrDefaultAsync(x => x.PhieuId == id);

            return new Phieu1ResponseDto
            {
                Phieu = phieu,
                ChiTiet = chiTiet.OrderBy(x => x.ThuTu).ToList(),
                KetLuan = ketLuan
            };
        }

        public async Task<Phieu1ResponseDto> ThemAsync(Phieu1Request request, int? nguoiTaoId)
        {
            var bepAn = await _bepAnRepository.GetByIdAsync(request.BepAnId)
                ?? throw new ApiException("Không tìm thấy bếp ăn", StatusCodes.Status404NotFound);
            if (!bepAn.NhaThauId.HasValue)
                throw new ApiException("Bếp ăn chưa gán nhà thầu vận hành, không thể lập phiếu");

            var phongBan = await _phongBanRepository.GetByIdAsync(request.PhongBanId)
                ?? throw new ApiException("Không tìm thấy phòng ban", StatusCodes.Status404NotFound);

            var ngay = request.NgayKiemTra.Date;
            var trung = await _phieu1Repository.FirstOrDefaultAsync(x =>
                x.NgayKiemTra == ngay && x.BepAnId == request.BepAnId && x.PhongBanId == request.PhongBanId);
            if (trung != null)
                throw new ApiException("Đã tồn tại phiếu kiểm tra cho bếp ăn, ngày và phòng ban này");

            var soHieu = await SinhSoHieuAsync(bepAn.Ma, phongBan.Ma, ngay);

            var phieu = new Phieu1KiemTra
            {
                SoHieu = soHieu,
                NgayKiemTra = ngay,
                BepAnId = request.BepAnId,
                NhaThauId = bepAn.NhaThauId.Value,
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

                foreach (var dong in request.ChiTiet)
                {
                    await _phieu1ChiTietRepository.AddAsync(new Phieu1ChiTiet
                    {
                        PhieuId = phieu.Id,
                        NhomId = dong.NhomId,
                        TieuChiId = dong.TieuChiId,
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

            return await ChiTietAsync(phieu.Id);
        }

        public async Task<Phieu1ResponseDto> SuaAsync(int id, Phieu1Request request)
        {
            var phieu = await _phieu1Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu kiểm tra", StatusCodes.Status404NotFound);

            if (phieu.TrangThai != "NHAP" && phieu.TrangThai != "TU_CHOI")
                throw new ApiException("Chỉ có thể sửa phiếu đang ở trạng thái Nháp hoặc bị Từ chối");

            var ngay = request.NgayKiemTra.Date;
            var trung = await _phieu1Repository.FirstOrDefaultAsync(x =>
                x.NgayKiemTra == ngay && x.BepAnId == request.BepAnId && x.PhongBanId == request.PhongBanId && x.Id != id);
            if (trung != null)
                throw new ApiException("Đã tồn tại phiếu kiểm tra cho bếp ăn, ngày và phòng ban này");

            phieu.NgayKiemTra = ngay;
            phieu.BepAnId = request.BepAnId;
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

                foreach (var dong in request.ChiTiet)
                {
                    if (dong.Id.HasValue && dong.Id > 0)
                    {
                        var dongHienTai = hienTai.FirstOrDefault(x => x.Id == dong.Id);
                        if (dongHienTai == null) continue;

                        dongHienTai.NhomId = dong.NhomId;
                        dongHienTai.TieuChiId = dong.TieuChiId;
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

            return await ChiTietAsync(id);
        }

        public async Task XoaAsync(int id)
        {
            var phieu = await _phieu1Repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phiếu kiểm tra", StatusCodes.Status404NotFound);

            if (phieu.TrangThai != "NHAP")
                throw new ApiException("Chỉ có thể xóa phiếu đang ở trạng thái Nháp");

            var chiTiet = await _phieu1ChiTietRepository.FindAsync(x => x.PhieuId == id);
            _phieu1ChiTietRepository.RemoveRange(chiTiet);

            var ketLuan = await _phieu1KetLuanRepository.FirstOrDefaultAsync(x => x.PhieuId == id);
            if (ketLuan != null)
                _phieu1KetLuanRepository.Remove(ketLuan);

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

        private async Task<string> SinhSoHieuAsync(string maBep, string maPhongBan, DateTime ngayKiemTra)
        {
            var nam = ngayKiemTra.Year;
            var thang = ngayKiemTra.Month;
            var seq = await _soHieuService.SinhSoTiepTheoAsync("PHIEU1", $"{maBep}-{maPhongBan}", nam, thang);
            return $"KT-{maBep}-{maPhongBan}-{thang:D2}{nam}-{seq:D3}";
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
