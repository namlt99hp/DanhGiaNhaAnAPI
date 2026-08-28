using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;

namespace DanhGiaAPI.Services
{
    public class PhieuNhaThauResolver : IPhieuNhaThauResolver
    {
        private readonly IPhieu1KiemTraRepository _phieu1Repository;
        private readonly IPhieu2DanhGiaRepository _phieu2Repository;
        private readonly IPhieu3BaoCaoRepository _phieu3Repository;

        public PhieuNhaThauResolver(
            IPhieu1KiemTraRepository phieu1Repository,
            IPhieu2DanhGiaRepository phieu2Repository,
            IPhieu3BaoCaoRepository phieu3Repository)
        {
            _phieu1Repository = phieu1Repository;
            _phieu2Repository = phieu2Repository;
            _phieu3Repository = phieu3Repository;
        }

        public async Task<int?> LayNhaThauIdAsync(string loaiPhieu, int doiTuongId)
        {
            switch (loaiPhieu)
            {
                case "PHIEU1":
                    return (await _phieu1Repository.GetByIdAsync(doiTuongId))?.NhaThauId;
                case "PHIEU2":
                    return (await _phieu2Repository.GetByIdAsync(doiTuongId))?.NhaThauId;
                case "PHIEU3":
                    return (await _phieu3Repository.GetByIdAsync(doiTuongId))?.NhaThauId;
                default:
                    // Phiếu 4 tổng hợp NHIỀU nhà thầu cùng lúc (Phieu4_NhaThau) —
                    // không có 1 NhaThauId duy nhất đại diện cho cả phiếu, nên
                    // LoaiNguoiKy = NHA_THAU không áp dụng được cho loại này.
                    return null;
            }
        }
    }
}
