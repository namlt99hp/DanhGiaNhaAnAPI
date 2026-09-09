using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.DiaDiemNhaAn;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class DiaDiemNhaAnService : IDiaDiemNhaAnService
    {
        private readonly IDiaDiemNhaAnRepository _diaDiemNhaAnRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DiaDiemNhaAnService(IDiaDiemNhaAnRepository diaDiemNhaAnRepository, IUnitOfWork unitOfWork)
        {
            _diaDiemNhaAnRepository = diaDiemNhaAnRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<DiaDiemNhaAn>> DanhSachAsync(bool? isActive)
        {
            var query = _diaDiemNhaAnRepository.Query();

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            return query.OrderBy(x => x.DiaDiem).ToList();
        }

        public async Task<DiaDiemNhaAn> ChiTietAsync(int id)
        {
            return await _diaDiemNhaAnRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy địa điểm nhà ăn", StatusCodes.Status404NotFound);
        }

        public async Task<DiaDiemNhaAn> ThemAsync(DiaDiemNhaAnRequest request)
        {
            var diaDiem = request.DiaDiem.Trim();
            if (await _diaDiemNhaAnRepository.GetByTenAsync(diaDiem) != null)
                throw new ApiException("Địa điểm nhà ăn đã tồn tại");

            var diaDiemNhaAn = new DiaDiemNhaAn
            {
                DiaDiem = diaDiem,
                CodeDiemAn = request.CodeDiemAn,
                IsActive = request.IsActive
            };
            await _diaDiemNhaAnRepository.AddAsync(diaDiemNhaAn);
            await _unitOfWork.SaveChangesAsync();
            return diaDiemNhaAn;
        }

        public async Task<DiaDiemNhaAn> SuaAsync(int id, DiaDiemNhaAnRequest request)
        {
            var diaDiemNhaAn = await _diaDiemNhaAnRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy địa điểm nhà ăn", StatusCodes.Status404NotFound);

            var diaDiem = request.DiaDiem.Trim();
            var trung = await _diaDiemNhaAnRepository.GetByTenAsync(diaDiem);
            if (trung != null && trung.ID != id)
                throw new ApiException("Địa điểm nhà ăn đã tồn tại");

            diaDiemNhaAn.DiaDiem = diaDiem;
            diaDiemNhaAn.CodeDiemAn = request.CodeDiemAn;
            diaDiemNhaAn.IsActive = request.IsActive;
            await _unitOfWork.SaveChangesAsync();
            return diaDiemNhaAn;
        }

        public async Task XoaAsync(int id)
        {
            var diaDiemNhaAn = await _diaDiemNhaAnRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy địa điểm nhà ăn", StatusCodes.Status404NotFound);

            _diaDiemNhaAnRepository.Remove(diaDiemNhaAn);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
