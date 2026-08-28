using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class NguoiDungService : INguoiDungService
    {
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly INguoiDungVaiTroRepository _nguoiDungVaiTroRepository;
        private readonly IVaiTroRepository _vaiTroRepository;
        private readonly IUnitOfWork _unitOfWork;

        public NguoiDungService(
            INguoiDungRepository nguoiDungRepository,
            INguoiDungVaiTroRepository nguoiDungVaiTroRepository,
            IVaiTroRepository vaiTroRepository,
            IUnitOfWork unitOfWork)
        {
            _nguoiDungRepository = nguoiDungRepository;
            _nguoiDungVaiTroRepository = nguoiDungVaiTroRepository;
            _vaiTroRepository = vaiTroRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<NguoiDungListItemDto>> DanhSachAsync(string? trangThai, int? phongBanId, int? nhaThauId)
        {
            // Nhiều filter tùy chọn -> dùng escape hatch Query() thay vì đẻ
            // 1 method riêng cho từng tổ hợp filter (xem IRepository<T>.Query()).
            var query = _nguoiDungRepository.Query();

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.TrangThai == trangThai);
            if (phongBanId.HasValue)
                query = query.Where(x => x.PhongBanId == phongBanId);
            if (nhaThauId.HasValue)
                query = query.Where(x => x.NhaThauId == nhaThauId);

            var danhSach = query.OrderByDescending(x => x.NgayTao).ToList();
            var ids = danhSach.Select(x => x.Id).ToList();

            var vaiTroMap = await _nguoiDungVaiTroRepository.GetVaiTroMapNhieuNguoiDungAsync(ids);

            return danhSach.Select(nd => MapToDto(
                nd,
                vaiTroMap.Where(x => x.NguoiDungId == nd.Id).Select(x => x.Ma).ToList()
            )).ToList();
        }

        public async Task<NguoiDungListItemDto> ChiTietAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            var vaiTro = await _nguoiDungVaiTroRepository.GetVaiTroCuaNguoiDungAsync(id);
            return MapToDto(nguoiDung, vaiTro.Select(x => x.Ma).ToList());
        }

        // Admin tạo trực tiếp 1 tài khoản (khác DangKyAsync ở AuthService — tự
        // đăng ký luôn CHO_DUYET): vào thẳng HOAT_DONG, NguoiDuyet/NgayDuyet ghi
        // nhận chính admin đang tạo, coi như đã "tự duyệt" tài khoản do mình tạo.
        public async Task<NguoiDungListItemDto> TaoTaiKhoanAsync(TaoTaiKhoanRequest request, int nguoiTaoId)
        {
            var tenDangNhap = request.TenDangNhap.Trim();

            if (await _nguoiDungRepository.GetByTenDangNhapAsync(tenDangNhap) != null)
                throw new ApiException("Tên đăng nhập đã tồn tại");

            if (!string.IsNullOrWhiteSpace(request.Email) && await _nguoiDungRepository.TonTaiEmailAsync(request.Email))
                throw new ApiException("Email đã được sử dụng");

            var nguoiDung = new NguoiDung
            {
                TenDangNhap = tenDangNhap,
                MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword(request.MatKhau),
                HoTen = request.HoTen.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                SoDienThoai = request.SoDienThoai,
                PhongBanId = request.PhongBanId,
                NhaThauId = request.NhaThauId,
                TrangThai = "HOAT_DONG",
                NguoiDuyet = nguoiTaoId,
                NgayDuyet = DateTime.Now,
                NgayTao = DateTime.Now
            };

            await _nguoiDungRepository.AddAsync(nguoiDung);
            await _unitOfWork.SaveChangesAsync();

            if (request.VaiTroIds.Count > 0)
            {
                var idHopLe = await _vaiTroRepository.GetExistingIdsAsync(request.VaiTroIds);
                await _nguoiDungVaiTroRepository.AddRangeAsync(idHopLe.Select(vtId => new NguoiDungVaiTro { NguoiDungId = nguoiDung.Id, VaiTroId = vtId }));
                await _unitOfWork.SaveChangesAsync();
            }

            return await ChiTietAsync(nguoiDung.Id);
        }

        public async Task DuyetAsync(int id, int nguoiDuyetId)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            if (nguoiDung.TrangThai != "CHO_DUYET")
                throw new ApiException("Tài khoản không ở trạng thái chờ duyệt");

            nguoiDung.TrangThai = "HOAT_DONG";
            nguoiDung.NguoiDuyet = nguoiDuyetId;
            nguoiDung.NgayDuyet = DateTime.Now;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task TuChoiAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            if (nguoiDung.TrangThai != "CHO_DUYET")
                throw new ApiException("Chỉ có thể từ chối tài khoản đang chờ duyệt");

            _nguoiDungRepository.Remove(nguoiDung);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task KhoaAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            if (nguoiDung.TrangThai == "CHO_DUYET")
                throw new ApiException("Tài khoản chưa được duyệt, không thể khóa");
            if (nguoiDung.TrangThai == "KHOA")
                throw new ApiException("Tài khoản đã bị khóa từ trước");

            nguoiDung.TrangThai = "KHOA";
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task MoKhoaAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            if (nguoiDung.TrangThai != "KHOA")
                throw new ApiException("Tài khoản không ở trạng thái khóa");

            nguoiDung.TrangThai = "HOAT_DONG";
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CapNhatVaiTroAsync(int id, List<int> vaiTroIds)
        {
            await TimHoacLoiAsync(id);

            var idHopLe = await _vaiTroRepository.GetExistingIdsAsync(vaiTroIds);

            var hienTai = await _nguoiDungVaiTroRepository.GetByNguoiDungIdAsync(id);
            _nguoiDungVaiTroRepository.RemoveRange(hienTai);
            await _nguoiDungVaiTroRepository.AddRangeAsync(idHopLe.Select(vtId => new NguoiDungVaiTro { NguoiDungId = id, VaiTroId = vtId }));

            await _unitOfWork.SaveChangesAsync();
        }

        // Mật khẩu mặc định khi Admin reset mật khẩu cho tài khoản khác (nhớ
        // đổi lại sau khi đăng nhập — chưa có cờ "bắt buộc đổi mật khẩu").
        private const string MatKhauMacDinh = "HPDQ@1234";

        public async Task<NguoiDungListItemDto> CapNhatProfileAsync(int id, CapNhatProfileRequest request)
        {
            var nguoiDung = await TimHoacLoiAsync(id);

            var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
            if (email != null && email != nguoiDung.Email && await _nguoiDungRepository.TonTaiEmailAsync(email))
                throw new ApiException("Email đã được sử dụng");

            nguoiDung.HoTen = request.HoTen.Trim();
            nguoiDung.Email = email;
            nguoiDung.SoDienThoai = request.SoDienThoai;
            await _unitOfWork.SaveChangesAsync();

            return await ChiTietAsync(id);
        }

        public async Task DoiMatKhauAsync(int id, DoiMatKhauRequest request)
        {
            var nguoiDung = await TimHoacLoiAsync(id);

            if (!BCrypt.Net.BCrypt.Verify(request.MatKhauHienTai, nguoiDung.MatKhauMaHoa))
                throw new ApiException("Mật khẩu hiện tại không đúng");

            nguoiDung.MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword(request.MatKhauMoi);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ResetMatKhauAsync(int id)
        {
            var nguoiDung = await TimHoacLoiAsync(id);
            nguoiDung.MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword(MatKhauMacDinh);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task<NguoiDung> TimHoacLoiAsync(int id)
        {
            return await _nguoiDungRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy tài khoản", StatusCodes.Status404NotFound);
        }

        private static NguoiDungListItemDto MapToDto(NguoiDung nd, List<string> maVaiTro) => new()
        {
            Id = nd.Id,
            TenDangNhap = nd.TenDangNhap,
            HoTen = nd.HoTen,
            Email = nd.Email,
            SoDienThoai = nd.SoDienThoai,
            PhongBanId = nd.PhongBanId,
            NhaThauId = nd.NhaThauId,
            TrangThai = nd.TrangThai,
            NguoiDuyet = nd.NguoiDuyet,
            NgayDuyet = nd.NgayDuyet,
            NgayTao = nd.NgayTao,
            DanhSachVaiTro = maVaiTro
        };
    }
}
