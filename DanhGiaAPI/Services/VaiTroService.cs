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
        private readonly IUnitOfWork _unitOfWork;

        public VaiTroService(IVaiTroRepository vaiTroRepository, IUnitOfWork unitOfWork)
        {
            _vaiTroRepository = vaiTroRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<VaiTro>> DanhSachAsync() => await _vaiTroRepository.GetAllAsync();

        public async Task<VaiTro> ThemAsync(VaiTroRequest request)
        {
            var ma = request.Ma.Trim();
            if (await _vaiTroRepository.GetByMaAsync(ma) != null)
                throw new ApiException("Mã vai trò đã tồn tại");

            var vaiTro = new VaiTro { Ma = ma, Ten = request.Ten.Trim(), CoQuyenDuyetTk = request.CoQuyenDuyetTk };
            await _vaiTroRepository.AddAsync(vaiTro);
            await _unitOfWork.SaveChangesAsync();
            return vaiTro;
        }

        public async Task<VaiTro> SuaAsync(int id, VaiTroRequest request)
        {
            var vaiTro = await _vaiTroRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy vai trò", StatusCodes.Status404NotFound);

            var ma = request.Ma.Trim();
            var trung = await _vaiTroRepository.GetByMaAsync(ma);
            if (trung != null && trung.Id != id)
                throw new ApiException("Mã vai trò đã tồn tại");

            vaiTro.Ma = ma;
            vaiTro.Ten = request.Ten.Trim();
            vaiTro.CoQuyenDuyetTk = request.CoQuyenDuyetTk;
            await _unitOfWork.SaveChangesAsync();
            return vaiTro;
        }

        public async Task XoaAsync(int id)
        {
            var vaiTro = await _vaiTroRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy vai trò", StatusCodes.Status404NotFound);

            _vaiTroRepository.Remove(vaiTro);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
