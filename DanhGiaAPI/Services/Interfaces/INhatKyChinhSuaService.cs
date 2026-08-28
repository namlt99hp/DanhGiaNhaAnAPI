using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface INhatKyChinhSuaService
    {
        // Gọi từ Service của Phiếu 3/4 mỗi khi số liệu tự tính bị sửa tay
        // (ChinhSuaThuCong = 1) — không có endpoint POST riêng vì client
        // không được tự ghi audit log, chỉ Service nghiệp vụ mới gọi.
        Task GhiAsync(string loaiDoiTuong, int doiTuongId, string? tenTruong, string? giaTriCu, string? giaTriMoi, int? nguoiThayDoi);

        Task<List<NhatKyChinhSua>> LayTheoDoiTuongAsync(string loaiDoiTuong, int doiTuongId);
    }
}
