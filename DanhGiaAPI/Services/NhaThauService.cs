using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.NhaThau;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class NhaThauService : INhaThauService
    {
        private readonly INhaThauRepository _nhaThauRepository;
        private readonly IUnitOfWork _unitOfWork;

        public NhaThauService(INhaThauRepository nhaThauRepository, IUnitOfWork unitOfWork)
        {
            _nhaThauRepository = nhaThauRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<NhaThau>> DanhSachAsync(string? trangThai)
        {
            if (!string.IsNullOrWhiteSpace(trangThai))
                return await _nhaThauRepository.FindAsync(x => x.TrangThai == trangThai);
            return await _nhaThauRepository.GetAllAsync();
        }

        public async Task<NhaThau> ChiTietAsync(int id)
        {
            return await _nhaThauRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy nhà thầu", StatusCodes.Status404NotFound);
        }

        public async Task<NhaThau> ThemAsync(NhaThauRequest request)
        {
            var ma = request.Ma.Trim();
            if (await _nhaThauRepository.GetByMaAsync(ma) != null)
                throw new ApiException("Mã nhà thầu đã tồn tại");

            var nhaThau = new NhaThau
            {
                Ma = ma,
                Ten = request.Ten.Trim(),
                NgayBatDauHd = request.NgayBatDauHd,
                NgayKetThucHd = request.NgayKetThucHd,
                TrangThai = request.TrangThai,
                NgayTao = DateTime.Now
            };
            await _nhaThauRepository.AddAsync(nhaThau);
            await _unitOfWork.SaveChangesAsync();
            return nhaThau;
        }

        public async Task<NhaThau> SuaAsync(int id, NhaThauRequest request)
        {
            var nhaThau = await _nhaThauRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy nhà thầu", StatusCodes.Status404NotFound);

            var ma = request.Ma.Trim();
            var trung = await _nhaThauRepository.GetByMaAsync(ma);
            if (trung != null && trung.Id != id)
                throw new ApiException("Mã nhà thầu đã tồn tại");

            nhaThau.Ma = ma;
            nhaThau.Ten = request.Ten.Trim();
            nhaThau.NgayBatDauHd = request.NgayBatDauHd;
            nhaThau.NgayKetThucHd = request.NgayKetThucHd;
            nhaThau.TrangThai = request.TrangThai;
            await _unitOfWork.SaveChangesAsync();
            return nhaThau;
        }

        public async Task XoaAsync(int id)
        {
            var nhaThau = await _nhaThauRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy nhà thầu", StatusCodes.Status404NotFound);

            _nhaThauRepository.Remove(nhaThau);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
