using DanhGiaAPI.DTOs.Common;
using DanhGiaAPI.DTOs.Phieu2;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IPhieu2Service
    {
        // nhaThauCuaNguoiGoi: NULL với tài khoản nội bộ; có giá trị với tài
        // khoản nhà thầu -> ép lọc/kiểm tra về đúng nhà thầu đó (xem DangNhap.md).
        // tuNgay/denNgay lọc theo ThoiGianTu (ngày kiểm tra thật, khác Thang/Nam).
        // tuKhoa: lọc theo SoHieu. chiCuaToi: chỉ phiếu do nguoiDungId tạo.
        Task<PagedResultDto<Phieu2DanhSachItemDto>> DanhSachAsync(
            int? nhaThauId, int? bepAnId, int? thang, int? nam, string? trangThai, DateTime? tuNgay, DateTime? denNgay,
            string? tuKhoa, bool chiCuaToi, int page, int pageSize,
            int? nhaThauCuaNguoiGoi, int? nguoiDungId, bool laAdmin);

        Task<Phieu2ResponseDto> ChiTietAsync(int id, int? nhaThauCuaNguoiGoi, int? nguoiDungId, bool laAdmin);

        Task<Phieu2ResponseDto> ThemAsync(Phieu2Request request, int? nguoiTaoId, bool laAdmin);

        Task<Phieu2ResponseDto> SuaAsync(int id, Phieu2Request request);

        // laAdmin bypass hoàn toàn ràng buộc "chỉ xóa được khi NHAP" — Admin có
        // thể xóa phiếu ở BẤT KỲ trạng thái nào từ trang danh sách, kèm dọn dẹp
        // dữ liệu luồng ký (ChuKyPhieu) liên quan — xem Phieu2Service.XoaAsync.
        Task XoaAsync(int id, bool laAdmin);

        // NHAP -> CHO_KY + khởi tạo luồng ký ĐẦY ĐỦ theo MauLuongKy (giống
        // Phiếu 3/4, xem IChuKyPhieuService.KhoiTaoLuongKyAsync) — thứ tự ký
        // (Nhà thầu trước, Phòng ban sau...) do Admin cấu hình BuocThuTu ở
        // màn "Luồng ký".
        Task<Phieu2DanhGia> GuiKyAsync(int id);

        // Đồng bộ TrangThai từ IChuKyPhieuService.TrangThaiTongAsync
        Task<Phieu2DanhGia> DongBoTrangThaiAsync(int id);

        // Cập nhật ý kiến phản hồi nhà thầu
        Task<Phieu2ResponseDto> PhanHoiYKienNhaThauAsync(int id, Phieu2YKienNhaThauRequest request);

        // Danh sách Phiếu 1 khả dụng để chọn liên kết — cùng nhà thầu, ĐÃ
        // DUYỆT, đúng phòng ban của người gọi (tùy chọn lọc thêm theo bếp ăn).
        Task<List<Phieu1KiemTra>> DanhSachPhieu1KhaDungAsync(int nhaThauId, int? bepAnId, int? phongBanCuaNguoiGoi);
    }
}
