using DanhGiaAPI.DTOs.Phieu1;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IPhieu1Service
    {
        Task<List<Phieu1KiemTra>> DanhSachAsync(int? bepAnId, int? phongBanId, int? nhaThauId, string? trangThai, DateTime? tuNgay, DateTime? denNgay);
        Task<Phieu1ResponseDto> ChiTietAsync(int id);
        Task<Phieu1ResponseDto> ThemAsync(Phieu1Request request, int? nguoiTaoId);
        Task<Phieu1ResponseDto> SuaAsync(int id, Phieu1Request request);
        Task XoaAsync(int id);

        // NHAP -> CHO_KY + khởi tạo luồng ký (xem IChuKyPhieuService.KhoiTaoLuongKyAsync)
        Task<Phieu1KiemTra> GuiKyAsync(int id);

        // Đọc IChuKyPhieuService.TrangThaiTongAsync rồi cập nhật TrangThai của
        // chính phiếu này — gọi sau khi FE ký/từ chối thành công qua endpoint
        // chung /api/chu-ky-phieu/{id}/ky|tu-choi (xem LuongTrinhKy.md).
        Task<Phieu1KiemTra> DongBoTrangThaiAsync(int id);
    }
}
