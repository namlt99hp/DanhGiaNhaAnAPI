using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.TieuChi;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class TieuChiService : ITieuChiService
    {
        private readonly ITieuChiRepository _tieuChiRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TieuChiService(ITieuChiRepository tieuChiRepository, IUnitOfWork unitOfWork)
        {
            _tieuChiRepository = tieuChiRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<TieuChi>> DanhSachAsync(int? nhomId, bool? dangHoatDong)
        {
            var query = _tieuChiRepository.Query();

            if (nhomId.HasValue)
                query = query.Where(x => x.NhomId == nhomId);
            if (dangHoatDong.HasValue)
                query = query.Where(x => x.DangHoatDong == dangHoatDong);

            return query.OrderBy(x => x.ThuTu).ToList();
        }

        public async Task<TieuChi> ChiTietAsync(int id)
        {
            return await _tieuChiRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy tiêu chí", StatusCodes.Status404NotFound);
        }

        public async Task<TieuChi> ThemAsync(TieuChiRequest request)
        {
            var tieuChi = new TieuChi
            {
                NhomId = request.NhomId,
                NoiDung = request.NoiDung.Trim(),
                ThuTu = request.ThuTu,
                MacDinh = request.MacDinh,
                DangHoatDong = request.DangHoatDong,
                Dvt = request.Dvt?.Trim(),
                LoaiDong = request.LoaiDong?.Trim(),
                CongThuc = request.CongThuc?.Trim()
            };
            await _tieuChiRepository.AddAsync(tieuChi);
            await _unitOfWork.SaveChangesAsync();
            return tieuChi;
        }

        public async Task<TieuChi> SuaAsync(int id, TieuChiRequest request)
        {
            var tieuChi = await _tieuChiRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy tiêu chí", StatusCodes.Status404NotFound);

            tieuChi.NhomId = request.NhomId;
            tieuChi.NoiDung = request.NoiDung.Trim();
            tieuChi.ThuTu = request.ThuTu;
            tieuChi.MacDinh = request.MacDinh;
            tieuChi.DangHoatDong = request.DangHoatDong;
            tieuChi.Dvt = request.Dvt?.Trim();
            tieuChi.LoaiDong = request.LoaiDong?.Trim();
            tieuChi.CongThuc = request.CongThuc?.Trim();
            await _unitOfWork.SaveChangesAsync();
            return tieuChi;
        }

        public async Task XoaAsync(int id)
        {
            var tieuChi = await _tieuChiRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy tiêu chí", StatusCodes.Status404NotFound);

            _tieuChiRepository.Remove(tieuChi);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
