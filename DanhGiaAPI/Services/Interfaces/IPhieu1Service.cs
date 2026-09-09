using DanhGiaAPI.DTOs.Phieu1;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IPhieu1Service
    {
        // nhaThauCuaNguoiGoi: NULL với tài khoản nội bộ (không lọc); có giá trị
        // với tài khoản nhà thầu -> ép lọc về đúng nhà thầu đó, bỏ qua nhaThauId
        // truyền vào nếu khác (xem DangNhap.md).
        Task<List<Phieu1KiemTra>> DanhSachAsync(int? bepAnId, int? phongBanId, int? nhaThauId, string? trangThai, DateTime? tuNgay, DateTime? denNgay, int? nhaThauCuaNguoiGoi);
        Task<Phieu1ResponseDto> ChiTietAsync(int id, int? nhaThauCuaNguoiGoi);
        Task<Phieu1ResponseDto> ThemAsync(Phieu1Request request, int? nguoiTaoId, bool laAdmin);
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
