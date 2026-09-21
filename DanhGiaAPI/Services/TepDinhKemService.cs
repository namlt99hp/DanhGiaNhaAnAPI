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

        private readonly ITepDinhKemRepository _tepDinhKemRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _env;

        public TepDinhKemService(ITepDinhKemRepository tepDinhKemRepository, IUnitOfWork unitOfWork, IWebHostEnvironment env)
        {
            _tepDinhKemRepository = tepDinhKemRepository;
            _unitOfWork = unitOfWork;
            _env = env;
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

            var duongDanDangDung = Regex.Matches(html, "src=\"([^\"]+)\"")
                .Select(m => m.Groups[1].Value)
                .Where(src => src.Contains($"/uploads/{ThuMucUpload}/"))
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

        private async Task<string> LuuFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ApiException("Chưa chọn file để tải lên");
            if (file.Length > DungLuongToiDa)
                throw new ApiException("File vượt quá 15MB");

            var duoi = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!DuoiChoPhep.Contains(duoi))
                throw new ApiException("Chỉ chấp nhận file ảnh .png, .jpg, .jpeg");

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var thuMuc = Path.Combine(webRoot, "uploads", ThuMucUpload);
            Directory.CreateDirectory(thuMuc);

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
