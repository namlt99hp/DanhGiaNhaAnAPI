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
        private readonly IUnitOfWork _unitOfWork;

        public PhongBanService(IPhongBanRepository phongBanRepository, IUnitOfWork unitOfWork)
        {
            _phongBanRepository = phongBanRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<PhongBan>> DanhSachAsync(bool? dangHoatDong)
        {
            if (dangHoatDong.HasValue)
                return await _phongBanRepository.FindAsync(x => x.DangHoatDong == dangHoatDong.Value);
            return await _phongBanRepository.GetAllAsync();
        }

        public async Task<PhongBan> ThemAsync(PhongBanRequest request)
        {
            var ma = request.Ma.Trim();
            if (await _phongBanRepository.GetByMaAsync(ma) != null)
                throw new ApiException("Mã phòng ban đã tồn tại");

            var phongBan = new PhongBan { Ma = ma, Ten = request.Ten.Trim(), DangHoatDong = request.DangHoatDong };
            await _phongBanRepository.AddAsync(phongBan);
            await _unitOfWork.SaveChangesAsync();
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
            return phongBan;
        }

        public async Task XoaAsync(int id)
        {
            var phongBan = await _phongBanRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy phòng ban", StatusCodes.Status404NotFound);

            _phongBanRepository.Remove(phongBan);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
