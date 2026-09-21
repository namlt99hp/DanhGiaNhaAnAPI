using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class PhongBanService : IPhongBanService
    {
        private readonly IPhongBanRepository _phongBanRepository;
        private readonly IPhongBanLoaiPhieuRepository _phongBanLoaiPhieuRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PhongBanService(
            IPhongBanRepository phongBanRepository,
            IPhongBanLoaiPhieuRepository phongBanLoaiPhieuRepository,
            IUnitOfWork unitOfWork)
        {
            _phongBanRepository = phongBanRepository;
            _phongBanLoaiPhieuRepository = phongBanLoaiPhieuRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<PhongBan>> DanhSachAsync(bool? dangHoatDong)
        {
            var danhSach = dangHoatDong.HasValue
                ? await _phongBanRepository.FindAsync(x => x.DangHoatDong == dangHoatDong.Value)
                : await _phongBanRepository.GetAllAsync();

            foreach (var phongBan in danhSach)
                phongBan.DanhSachLoaiPhieu = (await _phongBanLoaiPhieuRepository.GetByPhongBanIdAsync(phongBan.Id))
                    .Select(x => x.LoaiPhieu).ToList();

            return danhSach;
        }

        public async Task<PhongBan> ThemAsync(PhongBanRequest request)
        {
            var ma = request.Ma.Trim();
            if (await _phongBanRepository.GetByMaAsync(ma) != null)
                throw new ApiException("Mã phòng ban đã tồn tại");

            var phongBan = new PhongBan { Ma = ma, Ten = request.Ten.Trim(), DangHoatDong = request.DangHoatDong };
            await _phongBanRepository.AddAsync(phongBan);
            await _unitOfWork.SaveChangesAsync();

            await CapNhatLoaiPhieuApDungAsync(phongBan.Id, request.CacLoaiPhieuApDung);
            phongBan.DanhSachLoaiPhieu = request.CacLoaiPhieuApDung;
            return phongBan;
        }

        public async Task<PhongBan> SuaAsync(int id, PhongBanRequest request)
        {
            var phongBan = await _phongBanRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phòng ban", StatusCodes.Status404NotFound);

            var ma = request.Ma.Trim();
            var trung = await _phongBanRepository.GetByMaAsync(ma);
            if (trung != null && trung.Id != id)
                throw new ApiException("Mã phòng ban đã tồn tại");

            phongBan.Ma = ma;
            phongBan.Ten = request.Ten.Trim();
            phongBan.DangHoatDong = request.DangHoatDong;
            await _unitOfWork.SaveChangesAsync();

            await CapNhatLoaiPhieuApDungAsync(id, request.CacLoaiPhieuApDung);
            phongBan.DanhSachLoaiPhieu = request.CacLoaiPhieuApDung;
            return phongBan;
        }

        // Thay thế TOÀN BỘ tập loại phiếu áp dụng của 1 phòng ban — cùng cơ
        // chế "replace all" như CapNhatVaiTroAsync/CapNhatPhieuQuyenAsync.
        private async Task CapNhatLoaiPhieuApDungAsync(int phongBanId, List<string> danhSachLoaiPhieu)
        {
            var hienTai = await _phongBanLoaiPhieuRepository.GetByPhongBanIdAsync(phongBanId);
            _phongBanLoaiPhieuRepository.RemoveRange(hienTai);
            await _phongBanLoaiPhieuRepository.AddRangeAsync(
                danhSachLoaiPhieu.Distinct().Select(lp => new PhongBanLoaiPhieu { PhongBanId = phongBanId, LoaiPhieu = lp.Trim() }));
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task XoaAsync(int id)
        {
            var phongBan = await _phongBanRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phòng ban", StatusCodes.Status404NotFound);

            _phongBanLoaiPhieuRepository.RemoveRange(await _phongBanLoaiPhieuRepository.GetByPhongBanIdAsync(id));
            _phongBanRepository.Remove(phongBan);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
