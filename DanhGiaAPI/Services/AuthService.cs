using DanhGiaAPI.DTOs.Auth;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DanhGiaAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly INguoiDungVaiTroRepository _nguoiDungVaiTroRepository;
        private readonly IVaiTroRepository _vaiTroRepository;
        private readonly IPhienDangNhapRepository _phienDangNhapRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public AuthService(
            INguoiDungRepository nguoiDungRepository,
            INguoiDungVaiTroRepository nguoiDungVaiTroRepository,
            IVaiTroRepository vaiTroRepository,
            IPhienDangNhapRepository phienDangNhapRepository,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
        {
            _nguoiDungRepository = nguoiDungRepository;
            _nguoiDungVaiTroRepository = nguoiDungVaiTroRepository;
            _vaiTroRepository = vaiTroRepository;
            _phienDangNhapRepository = phienDangNhapRepository;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task DangKyAsync(DangKyRequest request)
        {
            var tenDangNhap = request.TenDangNhap.Trim();

            if (await _nguoiDungRepository.GetByTenDangNhapAsync(tenDangNhap) != null)
                throw new AuthException("Tên đăng nhập đã tồn tại");

            if (!string.IsNullOrWhiteSpace(request.Email) && await _nguoiDungRepository.TonTaiEmailAsync(request.Email))
                throw new AuthException("Email đã được sử dụng");

            // Tài khoản chỉ được thuộc 1 trong 2: Phòng ban (nội bộ) hoặc Nhà
            // thầu — cần cho đúng nhánh PHONG_BAN/NHA_THAU ở
            // ChuKyPhieuService.KiemTraQuyenKyAsync (xem VaiTro.md).
            if (request.PhongBanId.HasValue && request.NhaThauId.HasValue)
                throw new AuthException("Tài khoản chỉ được thuộc 1 trong 2: Phòng ban hoặc Nhà thầu, không được cả hai");

            var nguoiDung = new NguoiDung
            {
                TenDangNhap = tenDangNhap,
                MatKhauMaHoa = BCrypt.Net.BCrypt.HashPassword(request.MatKhau),
                HoTen = request.HoTen.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                SoDienThoai = request.SoDienThoai,
                PhongBanId = request.PhongBanId,
                NhaThauId = request.NhaThauId,
                TrangThai = "CHO_DUYET",
                NgayTao = DateTime.Now
            };

            await _nguoiDungRepository.AddAsync(nguoiDung);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<DangNhapResponseDto> DangNhapAsync(DangNhapRequest request, string? diaChiIp)
        {
            var tenDangNhap = request.TenDangNhap.Trim();
            var nguoiDung = await _nguoiDungRepository.GetByTenDangNhapAsync(tenDangNhap);

            if (nguoiDung == null || !BCrypt.Net.BCrypt.Verify(request.MatKhau, nguoiDung.MatKhauMaHoa))
                throw new AuthException("Sai tên đăng nhập hoặc mật khẩu", StatusCodes.Status401Unauthorized);

            if (nguoiDung.TrangThai == "CHO_DUYET")
                throw new AuthException("Tài khoản đang chờ duyệt", StatusCodes.Status403Forbidden);

            if (nguoiDung.TrangThai == "KHOA")
                throw new AuthException("Tài khoản đã bị khóa", StatusCodes.Status403Forbidden);

            var vaiTroCuaNguoiDung = await _nguoiDungVaiTroRepository.GetVaiTroCuaNguoiDungAsync(nguoiDung.Id);
            var maVaiTro = vaiTroCuaNguoiDung.Select(x => x.Ma).ToList();
            var laAdmin = vaiTroCuaNguoiDung.Any(x => x.LaQuanTriVien);
            var maQuyen = await _vaiTroRepository.GetMaQuyenChoNhieuVaiTroAsync(vaiTroCuaNguoiDung.Select(x => x.Id).ToList());

            var (token, hetHan) = TaoToken(nguoiDung, maVaiTro, laAdmin, maQuyen);

            await _phienDangNhapRepository.AddAsync(new PhienDangNhap
            {
                NguoiDungId = nguoiDung.Id,
                MaToken = token,
                DiaChiIp = diaChiIp,
                NgayTao = DateTime.Now,
                NgayHetHan = hetHan
            });
            await _unitOfWork.SaveChangesAsync();

            return new DangNhapResponseDto
            {
                Token = token,
                HetHan = hetHan,
                NguoiDung = new NguoiDungInfoDto
                {
                    Id = nguoiDung.Id,
                    TenDangNhap = nguoiDung.TenDangNhap,
                    HoTen = nguoiDung.HoTen,
                    Email = nguoiDung.Email,
                    PhongBanId = nguoiDung.PhongBanId,
                    NhaThauId = nguoiDung.NhaThauId,
                    TrangThai = nguoiDung.TrangThai,
                    DanhSachVaiTro = maVaiTro,
                    LaAdmin = laAdmin,
                    DanhSachQuyen = maQuyen
                }
            };
        }

        public async Task DangXuatAsync(string token)
        {
            var phien = await _phienDangNhapRepository.GetByTokenAsync(token);
            if (phien != null)
            {
                _phienDangNhapRepository.Remove(phien);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        // Sinh JWT thủ công thay vì dùng DB default cho NgayHetHan để controller/service
        // luôn có sẵn giá trị hết hạn trả về FE mà không cần round-trip đọc lại DB.
        private (string token, DateTime hetHan) TaoToken(NguoiDung nguoiDung, List<string> maVaiTro, bool laAdmin, List<string> maQuyen)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"]!;
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expiryMinutes = int.Parse(jwtSection["ExpiryMinutes"] ?? "480");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, nguoiDung.Id.ToString()),
                new Claim("ten_dang_nhap", nguoiDung.TenDangNhap),
                new Claim("ho_ten", nguoiDung.HoTen),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (nguoiDung.PhongBanId.HasValue)
                claims.Add(new Claim("phong_ban_id", nguoiDung.PhongBanId.Value.ToString()));
            if (nguoiDung.NhaThauId.HasValue)
                claims.Add(new Claim("nha_thau_id", nguoiDung.NhaThauId.Value.ToString()));

            claims.AddRange(maVaiTro.Select(ma => new Claim(ClaimTypes.Role, ma)));

            // "admin": bypass TOÀN BỘ policy quyền (xem CoQuyen() trong
            // Program.cs) — tính từ VaiTro.LaQuanTriVien = 1 bất kỳ, không
            // hard-code riêng vai trò ADMIN. "quyen": 1 claim cho mỗi mã quyền
            // vai trò của người dùng được gán qua VaiTroQuyen.
            if (laAdmin)
                claims.Add(new Claim("admin", "1"));
            claims.AddRange(maQuyen.Select(ma => new Claim("quyen", ma)));

            var hetHan = DateTime.Now.AddMinutes(expiryMinutes);
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: hetHan,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(jwt), hetHan);
        }
    }
}
