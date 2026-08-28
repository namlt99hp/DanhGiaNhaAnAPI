using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.TieuChi;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class NhomTieuChiService : INhomTieuChiService
    {
        private readonly INhomTieuChiRepository _nhomTieuChiRepository;
        private readonly IUnitOfWork _unitOfWork;

        public NhomTieuChiService(INhomTieuChiRepository nhomTieuChiRepository, IUnitOfWork unitOfWork)
        {
            _nhomTieuChiRepository = nhomTieuChiRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<NhomTieuChi>> DanhSachAsync(string? loaiPhieu, bool? dangHoatDong)
        {
            var query = _nhomTieuChiRepository.Query();

            if (!string.IsNullOrWhiteSpace(loaiPhieu))
                query = query.Where(x => x.LoaiPhieu == loaiPhieu);
            if (dangHoatDong.HasValue)
                query = query.Where(x => x.DangHoatDong == dangHoatDong);

            return query.OrderBy(x => x.ThuTu).ToList();
        }

        public async Task<NhomTieuChi> ChiTietAsync(int id)
        {
            return await _nhomTieuChiRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy nhóm tiêu chí", StatusCodes.Status404NotFound);
        }

        public async Task<NhomTieuChi> ThemAsync(NhomTieuChiRequest request)
        {
            var nhom = new NhomTieuChi
            {
                LoaiPhieu = request.LoaiPhieu.Trim(),
                Ma = request.Ma?.Trim(),
                Ten = request.Ten.Trim(),
                ThuTu = request.ThuTu,
                DangHoatDong = request.DangHoatDong,
                SoBang = request.SoBang
            };
            await _nhomTieuChiRepository.AddAsync(nhom);
            await _unitOfWork.SaveChangesAsync();
            return nhom;
        }

        public async Task<NhomTieuChi> SuaAsync(int id, NhomTieuChiRequest request)
        {
            var nhom = await _nhomTieuChiRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy nhóm tiêu chí", StatusCodes.Status404NotFound);

            nhom.LoaiPhieu = request.LoaiPhieu.Trim();
            nhom.Ma = request.Ma?.Trim();
            nhom.Ten = request.Ten.Trim();
            nhom.ThuTu = request.ThuTu;
            nhom.DangHoatDong = request.DangHoatDong;
            nhom.SoBang = request.SoBang;
            await _unitOfWork.SaveChangesAsync();
            return nhom;
        }

        public async Task XoaAsync(int id)
        {
            var nhom = await _nhomTieuChiRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy nhóm tiêu chí", StatusCodes.Status404NotFound);

            _nhomTieuChiRepository.Remove(nhom);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
