using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.BepAn;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class BepAnService : IBepAnService
    {
        private readonly IBepAnRepository _bepAnRepository;
        private readonly INhaThauRepository _nhaThauRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BepAnService(IBepAnRepository bepAnRepository, INhaThauRepository nhaThauRepository, IUnitOfWork unitOfWork)
        {
            _bepAnRepository = bepAnRepository;
            _nhaThauRepository = nhaThauRepository;
            _unitOfWork = unitOfWork;
        }

        // Nhà thầu vận hành chọn MỚI phải còn hoạt động (xem Common/DanhMucHoatDong.cs)
        private async Task KiemTraNhaThauAsync(int nhaThauId)
        {
            var nhaThau = await _nhaThauRepository.GetByIdAsync(nhaThauId)
                ?? throw new ApiException("Không tìm thấy nhà thầu");
            DanhMucHoatDong.KiemTraNhaThau(nhaThau);
        }

        public async Task<List<BepAn>> DanhSachAsync(int? nhaThauId, string? trangThai)
        {
            // Nhiều filter tùy chọn -> dùng escape hatch Query() (xem IRepository<T>.Query()).
            var query = _bepAnRepository.Query();

            if (nhaThauId.HasValue)
                query = query.Where(x => x.NhaThauId == nhaThauId);
            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.TrangThai == trangThai);

            return query.OrderBy(x => x.Ten).ToList();
        }

        public async Task<BepAn> ChiTietAsync(int id)
        {
            return await _bepAnRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy bếp ăn", StatusCodes.Status404NotFound);
        }

        public async Task<BepAn> ThemAsync(BepAnRequest request)
        {
            var ma = request.Ma.Trim();
            if (await _bepAnRepository.GetByMaAsync(ma) != null)
                throw new ApiException("Mã bếp ăn đã tồn tại");

            if (request.NhaThauId.HasValue)
                await KiemTraNhaThauAsync(request.NhaThauId.Value);

            var bepAn = new BepAn
            {
                Ma = ma,
                Ten = request.Ten.Trim(),
                ViTri = request.ViTri,
                NhaThauId = request.NhaThauId,
                TrangThai = request.TrangThai
            };
            await _bepAnRepository.AddAsync(bepAn);
            await _unitOfWork.SaveChangesAsync();
            return bepAn;
        }

        public async Task<BepAn> SuaAsync(int id, BepAnRequest request)
        {
            var bepAn = await _bepAnRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy bếp ăn", StatusCodes.Status404NotFound);

            var ma = request.Ma.Trim();
            var trung = await _bepAnRepository.GetByMaAsync(ma);
            if (trung != null && trung.Id != id)
                throw new ApiException("Mã bếp ăn đã tồn tại");

            // Giữ nguyên nhà thầu cũ (dù đã ngừng) vẫn lưu được — chỉ chặn khi đổi.
            if (request.NhaThauId.HasValue && request.NhaThauId != bepAn.NhaThauId)
                await KiemTraNhaThauAsync(request.NhaThauId.Value);

            bepAn.Ma = ma;
            bepAn.Ten = request.Ten.Trim();
            bepAn.ViTri = request.ViTri;
            bepAn.NhaThauId = request.NhaThauId;
            bepAn.TrangThai = request.TrangThai;
            await _unitOfWork.SaveChangesAsync();
            return bepAn;
        }

        public async Task XoaAsync(int id)
        {
            var bepAn = await _bepAnRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy bếp ăn", StatusCodes.Status404NotFound);

            _bepAnRepository.Remove(bepAn);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
