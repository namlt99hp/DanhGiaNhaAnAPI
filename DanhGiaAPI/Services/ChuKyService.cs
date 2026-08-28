using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.QuanLyTaiKhoan;
using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    public class ChuKyService : IChuKyService
    {
        private static readonly string[] DuoiChoPhep = { ".png", ".jpg", ".jpeg" };
        private const long DungLuongToiDa = 2 * 1024 * 1024; // 2MB

        private readonly IChuKyNguoiDungRepository _chuKyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _env;

        public ChuKyService(IChuKyNguoiDungRepository chuKyRepository, IUnitOfWork unitOfWork, IWebHostEnvironment env)
        {
            _chuKyRepository = chuKyRepository;
            _unitOfWork = unitOfWork;
            _env = env;
        }

        public async Task<ChuKyResponseDto> UploadAsync(int nguoiDungId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ApiException("Chưa chọn file chữ ký");
            if (file.Length > DungLuongToiDa)
                throw new ApiException("File chữ ký vượt quá 2MB");

            var duoi = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!DuoiChoPhep.Contains(duoi))
                throw new ApiException("Chỉ chấp nhận file ảnh .png, .jpg, .jpeg");

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var thuMuc = Path.Combine(webRoot, "uploads", "chu-ky");
            Directory.CreateDirectory(thuMuc);

            var tenFile = $"{Guid.NewGuid()}{duoi}";
            var duongDanVatLy = Path.Combine(thuMuc, tenFile);

            using (var stream = new FileStream(duongDanVatLy, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var duongDanChuKy = $"/uploads/chu-ky/{tenFile}";

            var hienTai = await _chuKyRepository.GetByNguoiDungIdAsync(nguoiDungId);
            foreach (var ck in hienTai)
                ck.DangSuDung = false;

            var moi = new ChuKyNguoiDung
            {
                NguoiDungId = nguoiDungId,
                DuongDanChuKy = duongDanChuKy,
                DangSuDung = true,
                NgayTao = DateTime.Now
            };
            await _chuKyRepository.AddAsync(moi);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(moi);
        }

        public async Task<List<ChuKyResponseDto>> DanhSachAsync(int nguoiDungId)
        {
            var danhSach = await _chuKyRepository.GetByNguoiDungIdAsync(nguoiDungId);
            return danhSach.Select(MapToDto).ToList();
        }

        public async Task KichHoatAsync(int nguoiDungId, int chuKyId)
        {
            var chuKy = await _chuKyRepository.GetByIdAndNguoiDungIdAsync(chuKyId, nguoiDungId)
                ?? throw new ApiException("Không tìm thấy chữ ký", StatusCodes.Status404NotFound);

            var hienTai = await _chuKyRepository.GetByNguoiDungIdAsync(nguoiDungId);
            foreach (var ck in hienTai)
                ck.DangSuDung = ck.Id == chuKyId;

            await _unitOfWork.SaveChangesAsync();
        }

        private static ChuKyResponseDto MapToDto(ChuKyNguoiDung ck) => new()
        {
            Id = ck.Id,
            DuongDanChuKy = ck.DuongDanChuKy,
            DangSuDung = ck.DangSuDung,
            NgayTao = ck.NgayTao
        };
    }
}
