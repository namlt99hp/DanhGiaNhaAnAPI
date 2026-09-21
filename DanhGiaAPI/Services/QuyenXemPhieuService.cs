using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;

namespace DanhGiaAPI.Services
{
    public class QuyenXemPhieuService : IQuyenXemPhieuService
    {
        private readonly INguoiDungPhieuQuyenRepository _nguoiDungPhieuQuyenRepository;
        private readonly INguoiDungMauLuongKyRepository _nguoiDungMauLuongKyRepository;
        private readonly IMauLuongKyRepository _mauLuongKyRepository;

        public QuyenXemPhieuService(
            INguoiDungPhieuQuyenRepository nguoiDungPhieuQuyenRepository,
            INguoiDungMauLuongKyRepository nguoiDungMauLuongKyRepository,
            IMauLuongKyRepository mauLuongKyRepository)
        {
            _nguoiDungPhieuQuyenRepository = nguoiDungPhieuQuyenRepository;
            _nguoiDungMauLuongKyRepository = nguoiDungMauLuongKyRepository;
            _mauLuongKyRepository = mauLuongKyRepository;
        }

        public async Task<bool> CoQuyenXemAsync(int nguoiDungId, string loaiPhieu) =>
            (await DanhSachLoaiPhieuDuocXemAsync(nguoiDungId)).Contains(loaiPhieu);

        public async Task<List<string>> DanhSachLoaiPhieuDuocXemAsync(int nguoiDungId)
        {
            var tuDanhGia = (await _nguoiDungPhieuQuyenRepository.GetByNguoiDungIdAsync(nguoiDungId))
                .Where(x => x.DuocDanhGia)
                .Select(x => x.LoaiPhieu);

            var mauLuongKyIds = (await _nguoiDungMauLuongKyRepository.GetByNguoiDungIdAsync(nguoiDungId))
                .Select(x => x.MauLuongKyId)
                .ToList();
            var tuKy = mauLuongKyIds.Count == 0
                ? Enumerable.Empty<string>()
                : _mauLuongKyRepository.Query().Where(x => mauLuongKyIds.Contains(x.Id)).Select(x => x.LoaiPhieu);

            return tuDanhGia.Union(tuKy).Distinct().ToList();
        }
    }
}
