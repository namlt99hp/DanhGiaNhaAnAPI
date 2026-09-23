using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.LuongKy;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class MauLuongKyService : IMauLuongKyService
    {
        private readonly IMauLuongKyRepository _mauLuongKyRepository;
        private readonly IPhongBanRepository _phongBanRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MauLuongKyService(IMauLuongKyRepository mauLuongKyRepository, IPhongBanRepository phongBanRepository, IUnitOfWork unitOfWork)
        {
            _mauLuongKyRepository = mauLuongKyRepository;
            _phongBanRepository = phongBanRepository;
            _unitOfWork = unitOfWork;
        }

        // Phòng ban ký chọn MỚI phải còn hoạt động (xem Common/DanhMucHoatDong.cs)
        private async Task KiemTraPhongBanAsync(int phongBanId)
        {
            var phongBan = await _phongBanRepository.GetByIdAsync(phongBanId)
                ?? throw new ApiException("Không tìm thấy phòng ban");
            DanhMucHoatDong.KiemTraPhongBan(phongBan);
        }

        public async Task<List<MauLuongKy>> DanhSachAsync(string? loaiPhieu)
        {
            var query = _mauLuongKyRepository.Query();

            if (!string.IsNullOrWhiteSpace(loaiPhieu))
                query = query.Where(x => x.LoaiPhieu == loaiPhieu);

            return query.OrderBy(x => x.LoaiPhieu).ThenBy(x => x.BuocThuTu).ToList();
        }

        public async Task<MauLuongKy> ChiTietAsync(int id)
        {
            return await _mauLuongKyRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy bước trong luồng ký", StatusCodes.Status404NotFound);
        }

        public async Task<MauLuongKy> ThemAsync(MauLuongKyRequest request)
        {
            KiemTraLoaiNguoiKy(request);
            if (request.PhongBanId.HasValue)
                await KiemTraPhongBanAsync(request.PhongBanId.Value);

            var mau = new MauLuongKy
            {
                LoaiPhieu = request.LoaiPhieu.Trim(),
                BuocThuTu = request.BuocThuTu,
                TenBuoc = request.TenBuoc.Trim(),
                LoaiNguoiKy = request.LoaiNguoiKy.Trim(),
                PhongBanId = request.PhongBanId,
                BatBuoc = request.BatBuoc
            };
            await _mauLuongKyRepository.AddAsync(mau);
            await _unitOfWork.SaveChangesAsync();
            return mau;
        }

        public async Task<MauLuongKy> SuaAsync(int id, MauLuongKyRequest request)
        {
            KiemTraLoaiNguoiKy(request);

            var mau = await _mauLuongKyRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy bước trong luồng ký", StatusCodes.Status404NotFound);

            // Giữ nguyên phòng ban cũ (dù đã ngừng) vẫn lưu được — chỉ chặn khi đổi.
            if (request.PhongBanId.HasValue && request.PhongBanId != mau.PhongBanId)
                await KiemTraPhongBanAsync(request.PhongBanId.Value);

            mau.LoaiPhieu = request.LoaiPhieu.Trim();
            mau.BuocThuTu = request.BuocThuTu;
            mau.TenBuoc = request.TenBuoc.Trim();
            mau.LoaiNguoiKy = request.LoaiNguoiKy.Trim();
            mau.PhongBanId = request.PhongBanId;
            mau.BatBuoc = request.BatBuoc;
            await _unitOfWork.SaveChangesAsync();
            return mau;
        }

        public async Task XoaAsync(int id)
        {
            var mau = await _mauLuongKyRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy bước trong luồng ký", StatusCodes.Status404NotFound);

            _mauLuongKyRepository.Remove(mau);
            await _unitOfWork.SaveChangesAsync();
        }

        private static readonly string[] LoaiNguoiKyHopLe = { "PHONG_BAN", "NHA_THAU", "TRUC_TIEP" };

        private static void KiemTraLoaiNguoiKy(MauLuongKyRequest request)
        {
            if (!LoaiNguoiKyHopLe.Contains(request.LoaiNguoiKy.Trim()))
                throw new ApiException("LoaiNguoiKy phải là PHONG_BAN, NHA_THAU hoặc TRUC_TIEP");
        }
    }
}
