using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class VaiTroService : IVaiTroService
    {
        private readonly IVaiTroRepository _vaiTroRepository;
        private readonly IVaiTroQuyenRepository _vaiTroQuyenRepository;
        private readonly IQuyenRepository _quyenRepository;
        private readonly INguoiDungVaiTroRepository _nguoiDungVaiTroRepository;
        private readonly IQuanTriGuardService _quanTriGuardService;
        private readonly IUnitOfWork _unitOfWork;

        public VaiTroService(
            IVaiTroRepository vaiTroRepository,
            IVaiTroQuyenRepository vaiTroQuyenRepository,
            IQuyenRepository quyenRepository,
            INguoiDungVaiTroRepository nguoiDungVaiTroRepository,
            IQuanTriGuardService quanTriGuardService,
            IUnitOfWork unitOfWork)
        {
            _vaiTroRepository = vaiTroRepository;
            _vaiTroQuyenRepository = vaiTroQuyenRepository;
            _quyenRepository = quyenRepository;
            _nguoiDungVaiTroRepository = nguoiDungVaiTroRepository;
            _quanTriGuardService = quanTriGuardService;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<VaiTroResponseDto>> DanhSachAsync()
        {
            var danhSach = await _vaiTroRepository.GetAllAsync();
            var result = new List<VaiTroResponseDto>();
            foreach (var vt in danhSach)
                result.Add(await MapToDtoAsync(vt));
            return result;
        }

        public async Task<VaiTroResponseDto> ThemAsync(VaiTroRequest request)
        {
            var ma = request.Ma.Trim();
            if (await _vaiTroRepository.GetByMaAsync(ma) != null)
                throw new ApiException("Mã vai trò đã tồn tại");

            var vaiTro = new VaiTro { Ma = ma, Ten = request.Ten.Trim(), LaQuanTriVien = request.LaQuanTriVien };
            await _vaiTroRepository.AddAsync(vaiTro);
            await _unitOfWork.SaveChangesAsync();

            await GhiQuyenAsync(vaiTro.Id, request.QuyenIds);
            await _unitOfWork.SaveChangesAsync();

            return await MapToDtoAsync(vaiTro);
        }

        public async Task<VaiTroResponseDto> SuaAsync(int id, VaiTroRequest request)
        {
            var vaiTro = await _vaiTroRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy vai trò", StatusCodes.Status404NotFound);

            var ma = request.Ma.Trim();
            var trung = await _vaiTroRepository.GetByMaAsync(ma);
            if (trung != null && trung.Id != id)
                throw new ApiException("Mã vai trò đã tồn tại");

            // Kiểm tra TRƯỚC khi ghi — nếu bỏ LaQuanTriVien/QUAN_LY_TAI_KHOAN của
            // vai trò này khiến không còn ai quản lý được tài khoản thì chặn lại.
            await _quanTriGuardService.KiemTraSauKhiSuaVaiTroAsync(id, request.LaQuanTriVien, request.QuyenIds);

            vaiTro.Ma = ma;
            vaiTro.Ten = request.Ten.Trim();
            vaiTro.LaQuanTriVien = request.LaQuanTriVien;
            await _unitOfWork.SaveChangesAsync();

            await GhiQuyenAsync(id, request.QuyenIds);
            await _unitOfWork.SaveChangesAsync();

            return await MapToDtoAsync(vaiTro);
        }

        public async Task XoaAsync(int id)
        {
            var vaiTro = await _vaiTroRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy vai trò", StatusCodes.Status404NotFound);

            if (await _nguoiDungVaiTroRepository.AnyAsync(x => x.VaiTroId == id))
                throw new ApiException("Không thể xóa vì vai trò này đang được gán cho ít nhất 1 tài khoản");

            var quyenHienTai = await _vaiTroQuyenRepository.GetByVaiTroIdAsync(id);
            _vaiTroQuyenRepository.RemoveRange(quyenHienTai);
            _vaiTroRepository.Remove(vaiTro);
            await _unitOfWork.SaveChangesAsync();
        }

        // Thay thế TOÀN BỘ tập quyền của 1 vai trò — cùng kiểu với
        // NguoiDungService.CapNhatVaiTroAsync (gửi thiếu 1 Id = mất quyền đó).
        private async Task GhiQuyenAsync(int vaiTroId, List<int> quyenIds)
        {
            var idHopLe = await _quyenRepository.GetExistingIdsAsync(quyenIds);

            var hienTai = await _vaiTroQuyenRepository.GetByVaiTroIdAsync(vaiTroId);
            _vaiTroQuyenRepository.RemoveRange(hienTai);
            await _vaiTroQuyenRepository.AddRangeAsync(idHopLe.Select(qId => new VaiTroQuyen { VaiTroId = vaiTroId, QuyenId = qId }));
        }

        private async Task<VaiTroResponseDto> MapToDtoAsync(VaiTro vt) => new()
        {
            Id = vt.Id,
            Ma = vt.Ma,
            Ten = vt.Ten,
            LaQuanTriVien = vt.LaQuanTriVien,
            QuyenIds = (await _vaiTroQuyenRepository.GetByVaiTroIdAsync(vt.Id)).Select(x => x.QuyenId).ToList()
        };
    }
}
