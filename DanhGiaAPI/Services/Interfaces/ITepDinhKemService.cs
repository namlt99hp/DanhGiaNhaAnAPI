using DanhGiaAPI.Entities;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface ITepDinhKemService
    {
        // Ảnh rời đính kèm minh chứng — LoaiDoiTuong/DoiTuongId biết ngay lúc upload.
        Task<TepDinhKem> UploadAsync(string loaiDoiTuong, int? doiTuongId, IFormFile file, int? nguoiTaiLen);

        // Ảnh nhúng CKEditor — chưa biết dòng cha lúc upload (DoiTuongId = NULL),
        // chốt liên kết sau qua ChotLienKetCkeditorAsync khi dòng cha được lưu thật sự.
        Task<TepDinhKem> UploadCkeditorAsync(IFormFile file, int? nguoiTaiLen);

        Task<List<TepDinhKem>> DanhSachAsync(string? loaiDoiTuong, int? doiTuongId);
        Task XoaAsync(int id);

        // Parse HTML tìm ảnh CKEditor đang được dùng (theo DuongDanTep) rồi UPDATE
        // DoiTuongId từ NULL sang doiTuongId của dòng cha vừa lưu.
        Task ChotLienKetCkeditorAsync(int doiTuongId, string? html);
    }
}
