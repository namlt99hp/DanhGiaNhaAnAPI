using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;

namespace DanhGiaAPI.Services
{
    public class NhatKyChinhSuaService : INhatKyChinhSuaService
    {
        private readonly INhatKyChinhSuaRepository _nhatKyChinhSuaRepository;
        private readonly IUnitOfWork _unitOfWork;

        public NhatKyChinhSuaService(INhatKyChinhSuaRepository nhatKyChinhSuaRepository, IUnitOfWork unitOfWork)
        {
            _nhatKyChinhSuaRepository = nhatKyChinhSuaRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task GhiAsync(string loaiDoiTuong, int doiTuongId, string? tenTruong, string? giaTriCu, string? giaTriMoi, int? nguoiThayDoi)
        {
            await _nhatKyChinhSuaRepository.AddAsync(new NhatKyChinhSua
            {
                LoaiDoiTuong = loaiDoiTuong,
                DoiTuongId = doiTuongId,
                TenTruong = tenTruong,
                GiaTriCu = giaTriCu,
                GiaTriMoi = giaTriMoi,
                NguoiThayDoi = nguoiThayDoi,
                NgayThayDoi = DateTime.Now
            });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<NhatKyChinhSua>> LayTheoDoiTuongAsync(string loaiDoiTuong, int doiTuongId) =>
            await _nhatKyChinhSuaRepository.LayTheoDoiTuongAsync(loaiDoiTuong, doiTuongId);
    }
}
