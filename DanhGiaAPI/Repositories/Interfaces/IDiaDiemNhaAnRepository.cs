using DanhGiaAPI.Models;

namespace DanhGiaAPI.Repositories.Interfaces
{
    // DiaDiemNhaAn = bảng "nhà ăn" (điểm ăn) có sẵn từ hệ thống chấm điểm bữa
    // ăn cũ (module 1, xem Models/DiaDiemNhaAn.cs) — Phiếu 2 dùng lại bảng
    // này làm danh mục Nhà ăn thay vì tạo bảng master data mới (đã xác nhận
    // nghiệp vụ 2026-08-27, xem modules/Phieu2_DanhGiaSuatAn.md).
    public interface IDiaDiemNhaAnRepository : IRepository<DiaDiemNhaAn>
    {
    }
}
