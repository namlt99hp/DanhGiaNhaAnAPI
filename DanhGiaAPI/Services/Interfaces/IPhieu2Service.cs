using DanhGiaAPI.DTOs.Phieu2;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IPhieu2Service
    {
        // nhaThauCuaNguoiGoi: NULL với tài khoản nội bộ; có giá trị với tài
        // khoản nhà thầu -> ép lọc/kiểm tra về đúng nhà thầu đó (xem DangNhap.md).
        Task<List<Phieu2DanhSachItemDto>> DanhSachAsync(int? nhaThauId, int? bepAnId, int? thang, int? nam, string? trangThai, int? nhaThauCuaNguoiGoi);

        Task<Phieu2ResponseDto> ChiTietAsync(int id, int? nhaThauCuaNguoiGoi);

        Task<Phieu2ResponseDto> ThemAsync(Phieu2Request request, int? nguoiTaoId, bool laAdmin);

        Task<Phieu2ResponseDto> SuaAsync(int id, Phieu2Request request);

        Task XoaAsync(int id);

        // NHAP -> CHO_KY: lưu trước rồi mới khởi tạo luồng ký
        Task<Phieu2DanhGia> GuiKyAsync(int id);

        // Đồng bộ TrangThai từ IChuKyPhieuService.TrangThaiTongAsync
        Task<Phieu2DanhGia> DongBoTrangThaiAsync(int id);

        // Cập nhật ý kiến phản hồi nhà thầu
        Task<Phieu2ResponseDto> PhanHoiYKienNhaThauAsync(int id, Phieu2YKienNhaThauRequest request);

        // Danh sách Phiếu 1 khả dụng để chọn liên kết (cùng nhà thầu, tùy chọn lọc thêm theo bếp ăn)
        Task<List<Phieu1KiemTra>> DanhSachPhieu1KhaDungAsync(int nhaThauId, int? bepAnId);
    }
}
