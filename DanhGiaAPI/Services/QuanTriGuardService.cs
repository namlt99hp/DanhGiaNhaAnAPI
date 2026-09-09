using DanhGiaAPI.Common;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;

namespace DanhGiaAPI.Services
{
    public class QuanTriGuardService : IQuanTriGuardService
    {
        private const string MaQuyenQuanLyTaiKhoan = "QUAN_LY_TAI_KHOAN";

        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly INguoiDungVaiTroRepository _nguoiDungVaiTroRepository;
        private readonly IVaiTroRepository _vaiTroRepository;
        private readonly IVaiTroQuyenRepository _vaiTroQuyenRepository;
        private readonly IQuyenRepository _quyenRepository;

        public QuanTriGuardService(
            INguoiDungRepository nguoiDungRepository,
            INguoiDungVaiTroRepository nguoiDungVaiTroRepository,
            IVaiTroRepository vaiTroRepository,
            IVaiTroQuyenRepository vaiTroQuyenRepository,
            IQuyenRepository quyenRepository)
        {
            _nguoiDungRepository = nguoiDungRepository;
            _nguoiDungVaiTroRepository = nguoiDungVaiTroRepository;
            _vaiTroRepository = vaiTroRepository;
            _vaiTroQuyenRepository = vaiTroQuyenRepository;
            _quyenRepository = quyenRepository;
        }

        public Task KiemTraSauKhiDoiVaiTroNguoiDungAsync(int nguoiDungId, List<int> vaiTroIdsMoi)
        {
            var vaiTroIdsTheoNguoiDung = LayVaiTroIdTheoNguoiDungHoatDong();
            vaiTroIdsTheoNguoiDung[nguoiDungId] = vaiTroIdsMoi.ToHashSet();

            var (dinhNghiaVaiTro, quanLyTaiKhoanId) = LayDinhNghiaVaiTro();

            if (!ConAiQuanTri(vaiTroIdsTheoNguoiDung, dinhNghiaVaiTro, quanLyTaiKhoanId))
                throw new ApiException("Không thể thực hiện vì sẽ không còn ai có quyền quản lý tài khoản trong hệ thống.");

            return Task.CompletedTask;
        }

        public Task KiemTraSauKhiSuaVaiTroAsync(int vaiTroId, bool laQuanTriVienMoi, List<int> quyenIdsMoi)
        {
            var vaiTroIdsTheoNguoiDung = LayVaiTroIdTheoNguoiDungHoatDong();
            var (dinhNghiaVaiTro, quanLyTaiKhoanId) = LayDinhNghiaVaiTro();

            dinhNghiaVaiTro[vaiTroId] = (laQuanTriVienMoi, quyenIdsMoi.ToHashSet());

            if (!ConAiQuanTri(vaiTroIdsTheoNguoiDung, dinhNghiaVaiTro, quanLyTaiKhoanId))
                throw new ApiException("Không thể thực hiện vì sẽ không còn vai trò nào có quyền quản lý tài khoản trong hệ thống.");

            return Task.CompletedTask;
        }

        private Dictionary<int, HashSet<int>> LayVaiTroIdTheoNguoiDungHoatDong()
        {
            var nguoiDungHoatDongIds = _nguoiDungRepository.Query()
                .Where(x => x.TrangThai == "HOAT_DONG")
                .Select(x => x.Id)
                .ToHashSet();

            var ketQua = nguoiDungHoatDongIds.ToDictionary(id => id, _ => new HashSet<int>());

            foreach (var ndvt in _nguoiDungVaiTroRepository.Query())
            {
                if (ketQua.TryGetValue(ndvt.NguoiDungId, out var vaiTroIds))
                    vaiTroIds.Add(ndvt.VaiTroId);
            }

            return ketQua;
        }

        private (Dictionary<int, (bool LaQuanTriVien, HashSet<int> QuyenIds)> DinhNghia, int? QuanLyTaiKhoanId) LayDinhNghiaVaiTro()
        {
            var dinhNghia = _vaiTroRepository.Query()
                .ToDictionary(vt => vt.Id, vt => (LaQuanTriVien: vt.LaQuanTriVien, QuyenIds: new HashSet<int>()));

            foreach (var vtq in _vaiTroQuyenRepository.Query())
            {
                if (dinhNghia.TryGetValue(vtq.VaiTroId, out var dn))
                    dn.QuyenIds.Add(vtq.QuyenId);
            }

            var quanLyTaiKhoanId = _quyenRepository.Query()
                .Where(x => x.Ma == MaQuyenQuanLyTaiKhoan)
                .Select(x => (int?)x.Id)
                .FirstOrDefault();

            return (dinhNghia, quanLyTaiKhoanId);
        }

        private static bool ConAiQuanTri(
            Dictionary<int, HashSet<int>> vaiTroIdsTheoNguoiDung,
            Dictionary<int, (bool LaQuanTriVien, HashSet<int> QuyenIds)> dinhNghiaVaiTro,
            int? quanLyTaiKhoanId)
        {
            foreach (var vaiTroIds in vaiTroIdsTheoNguoiDung.Values)
            {
                foreach (var vaiTroId in vaiTroIds)
                {
                    if (!dinhNghiaVaiTro.TryGetValue(vaiTroId, out var dn))
                        continue;

                    if (dn.LaQuanTriVien)
                        return true;

                    if (quanLyTaiKhoanId.HasValue && dn.QuyenIds.Contains(quanLyTaiKhoanId.Value))
                        return true;
                }
            }

            return false;
        }
    }
}
