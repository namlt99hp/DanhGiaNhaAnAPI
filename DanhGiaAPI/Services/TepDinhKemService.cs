using System.Text.RegularExpressions;
using DanhGiaAPI.Common;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class TepDinhKemService : ITepDinhKemService
    {
        private const string ThuMucUpload = "dinh-kem";
        private const string LoaiDoiTuongCkeditor = "GHI_CHU_CKEDITOR";
        private static readonly string[] DuoiChoPhep = { ".png", ".jpg", ".jpeg" };
        // Ảnh chụp thẳng từ camera điện thoại thường nặng hơn nhiều so với ảnh
        // chụp màn hình (có thể 8-15MB tùy độ phân giải cảm biến) — nâng lên
        // 15MB để không chặn nhầm ảnh hợp lệ. Nhớ nâng client_max_body_size
        // tương ứng ở nginx phía trước, nếu không nginx sẽ tự cắt kết nối
        // trước khi request tới được đây (FE sẽ thấy "Lỗi mạng" chứ không phải
        // message lỗi rõ ràng bên dưới).
        private const long DungLuongToiDa = 15 * 1024 * 1024; // 15MB

        // Ảnh base64 nhúng thẳng trong HTML — lọt vào khi bấm lưu ghi chú lúc
        // ảnh chưa upload xong/upload lỗi (TinyMCE serialize ảnh blob chưa
        // upload thành base64 của ẢNH GỐC chưa nén, mỗi ảnh vài MB -> GET
        // phiếu nặng hàng chục MB). Xem TachAnhBase64Async.
        private static readonly Regex AnhBase64Regex = new(
            "src=\"data:image/(png|jpe?g|gif|webp);base64,([^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ITepDinhKemRepository _tepDinhKemRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TepDinhKemService(
            ITepDinhKemRepository tepDinhKemRepository,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment env,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _tepDinhKemRepository = tepDinhKemRepository;
            _unitOfWork = unitOfWork;
            _env = env;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<TepDinhKem> UploadAsync(string loaiDoiTuong, int? doiTuongId, IFormFile file, int? nguoiTaiLen)
        {
            var duongDan = await LuuFileAsync(file);

            var tep = new TepDinhKem
            {
                LoaiDoiTuong = loaiDoiTuong,
                DoiTuongId = doiTuongId,
                DuongDanTep = duongDan,
                TenTep = file.FileName,
                NguoiTaiLen = nguoiTaiLen,
                NgayTaiLen = DateTime.Now
            };
            await _tepDinhKemRepository.AddAsync(tep);
            await _unitOfWork.SaveChangesAsync();
            return tep;
        }

        public async Task<TepDinhKem> UploadCkeditorAsync(IFormFile file, int? nguoiTaiLen)
        {
            return await UploadAsync(LoaiDoiTuongCkeditor, null, file, nguoiTaiLen);
        }

        public async Task<List<TepDinhKem>> DanhSachAsync(string? loaiDoiTuong, int? doiTuongId)
        {
            var query = _tepDinhKemRepository.Query();

            if (!string.IsNullOrWhiteSpace(loaiDoiTuong))
                query = query.Where(x => x.LoaiDoiTuong == loaiDoiTuong);
            if (doiTuongId.HasValue)
                query = query.Where(x => x.DoiTuongId == doiTuongId);

            return query.OrderByDescending(x => x.NgayTaiLen).ToList();
        }

        public async Task XoaAsync(int id)
        {
            var tep = await _tepDinhKemRepository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy tệp đính kèm", StatusCodes.Status404NotFound);

            var duongDanVatLy = ToDuongDanVatLy(tep.DuongDanTep);
            if (File.Exists(duongDanVatLy))
                File.Delete(duongDanVatLy);

            _tepDinhKemRepository.Remove(tep);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ChotLienKetCkeditorAsync(int doiTuongId, string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return;

            // src trong HTML là URL TUYỆT ĐỐI (FE ghép ApiRootV2, VD
            // https://apinhaan.../uploads/dinh-kem/x.jpg) còn DuongDanTep lưu
            // tương đối (/uploads/dinh-kem/x.jpg) — cắt về phần tương đối mới
            // so khớp được (trước đây so nguyên chuỗi nên không bao giờ chốt).
            var tienToUpload = $"/uploads/{ThuMucUpload}/";
            var duongDanDangDung = Regex.Matches(html, "src=\"([^\"]+)\"")
                .Select(m => m.Groups[1].Value)
                .Where(src => src.Contains(tienToUpload))
                .Select(src => src[src.IndexOf(tienToUpload, StringComparison.Ordinal)..])
                .ToHashSet();

            if (duongDanDangDung.Count == 0)
                return;

            var danhSachChuaChot = await Task.FromResult(
                _tepDinhKemRepository.Query()
                    .Where(x => x.LoaiDoiTuong == LoaiDoiTuongCkeditor && x.DoiTuongId == null)
                    .Where(x => duongDanDangDung.Contains(x.DuongDanTep))
                    .ToList());

            foreach (var tep in danhSachChuaChot)
            {
                tep.DoiTuongId = doiTuongId;
                _tepDinhKemRepository.Update(tep);
            }

            if (danhSachChuaChot.Count > 0)
                await _unitOfWork.SaveChangesAsync();
        }

        public async Task<string?> TachAnhBase64Async(string? html, int? nguoiTaiLen)
        {
            if (string.IsNullOrEmpty(html) || !html.Contains("data:image", StringComparison.OrdinalIgnoreCase))
                return html;

            var thuMuc = LayThuMucUpload();
            var urlGoc = LayUrlGocApi();
            var ketQua = new System.Text.StringBuilder(html.Length);
            var viTri = 0;
            var daTach = 0;

            foreach (Match m in AnhBase64Regex.Matches(html))
            {
                byte[] noiDung;
                try
                {
                    noiDung = Convert.FromBase64String(m.Groups[2].Value);
                }
                catch (FormatException)
                {
                    continue; // base64 hỏng — để nguyên, không làm hỏng cả ghi chú
                }

                var duoi = m.Groups[1].Value.ToLowerInvariant() switch
                {
                    "jpeg" or "jpg" => ".jpg",
                    var x => "." + x
                };
                var tenFile = $"{Guid.NewGuid()}{duoi}";
                await File.WriteAllBytesAsync(Path.Combine(thuMuc, tenFile), noiDung);

                var duongDan = $"/uploads/{ThuMucUpload}/{tenFile}";
                await _tepDinhKemRepository.AddAsync(new TepDinhKem
                {
                    LoaiDoiTuong = LoaiDoiTuongCkeditor,
                    DoiTuongId = null,
                    DuongDanTep = duongDan,
                    TenTep = tenFile,
                    NguoiTaiLen = nguoiTaiLen,
                    NgayTaiLen = DateTime.Now
                });

                ketQua.Append(html, viTri, m.Index - viTri);
                ketQua.Append($"src=\"{urlGoc}{duongDan}\"");
                viTri = m.Index + m.Length;
                daTach++;
            }

            if (daTach == 0) return html;

            ketQua.Append(html, viTri, html.Length - viTri);
            await _unitOfWork.SaveChangesAsync();
            return ketQua.ToString();
        }

        // Gốc URL API công khai để ghép vào src ảnh, đúng định dạng FE đang
        // nhúng (ApiRootV2 + /uploads/...). Ưu tiên cấu hình "UrlGocApi" (VD
        // https://apinhaan.hoaphatdungquat.vn) — chạy sau nginx mà nginx không
        // forward Host/Proto thì Request.Host là địa chỉ nội bộ, sai. Không
        // cấu hình thì lấy theo request hiện tại.
        private string LayUrlGocApi()
        {
            var cauHinh = _configuration["UrlGocApi"];
            if (!string.IsNullOrWhiteSpace(cauHinh)) return cauHinh.TrimEnd('/');

            var request = _httpContextAccessor.HttpContext?.Request;
            return request == null ? "" : $"{request.Scheme}://{request.Host}";
        }

        private string LayThuMucUpload()
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var thuMuc = Path.Combine(webRoot, "uploads", ThuMucUpload);
            Directory.CreateDirectory(thuMuc);
            return thuMuc;
        }

        private async Task<string> LuuFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ApiException("Chưa chọn file để tải lên");
            if (file.Length > DungLuongToiDa)
                throw new ApiException("File vượt quá 15MB");

            var duoi = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!DuoiChoPhep.Contains(duoi))
                throw new ApiException("Chỉ chấp nhận file ảnh .png, .jpg, .jpeg");

            var thuMuc = LayThuMucUpload();

            var tenFile = $"{Guid.NewGuid()}{duoi}";
            var duongDanVatLy = Path.Combine(thuMuc, tenFile);

            using (var stream = new FileStream(duongDanVatLy, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{ThuMucUpload}/{tenFile}";
        }

        private string ToDuongDanVatLy(string duongDanTep)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            return Path.Combine(webRoot, duongDanTep.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
