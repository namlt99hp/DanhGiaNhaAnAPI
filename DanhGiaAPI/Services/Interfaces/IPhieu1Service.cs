using DanhGiaAPI.DTOs.Common;
using DanhGiaAPI.DTOs.Phieu1;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IPhieu1Service
    {
        // nhaThauCuaNguoiGoi: NULL với tài khoản nội bộ (không lọc); có giá trị
        // với tài khoản nhà thầu -> ép lọc về đúng nhà thầu đó, bỏ qua nhaThauId
        // truyền vào nếu khác (xem DangNhap.md). tuKhoa: lọc theo SoHieu (chứa
        // chuỗi). chiCuaToi: chỉ phiếu do nguoiDungId tạo. page/pageSize: phân
        // trang server-side (page bắt đầu từ 1). phongBanCuaNguoiGoi: phòng ban
        // của tài khoản NỘI BỘ đang gọi — tài khoản nội bộ không phải Admin chỉ
        // thấy phiếu NHAP/CHO_KY/TU_CHOI của ĐÚNG phòng ban mình, phiếu DA_DUYET
        // thì phòng ban nào cũng thấy (xem Phieu1Service.DanhSachAsync).
        Task<PagedResultDto<Phieu1DanhSachItemDto>> DanhSachAsync(
            int? bepAnId, int? phongBanId, int? nhaThauId, string? trangThai, DateTime? tuNgay, DateTime? denNgay,
            string? tuKhoa, bool chiCuaToi, int page, int pageSize,
            int? nhaThauCuaNguoiGoi, int? nguoiDungId, bool laAdmin, int? phongBanCuaNguoiGoi);
        Task<Phieu1ResponseDto> ChiTietAsync(int id, int? nhaThauCuaNguoiGoi, int? nguoiDungId, bool laAdmin, int? phongBanCuaNguoiGoi);
        Task<Phieu1ResponseDto> ThemAsync(Phieu1Request request, int? nguoiTaoId, bool laAdmin);
        Task<Phieu1ResponseDto> SuaAsync(int id, Phieu1Request request);

        // laAdmin bypass hoàn toàn ràng buộc "chỉ xóa được khi NHAP" — Admin có
        // thể xóa phiếu ở BẤT KỲ trạng thái nào từ trang danh sách, kèm dọn dẹp
        // dữ liệu luồng ký (ChuKyPhieu) liên quan — xem Phieu1Service.XoaAsync.
        Task XoaAsync(int id, bool laAdmin);

        // NHAP -> CHO_KY + khởi tạo luồng ký ĐẦY ĐỦ theo MauLuongKy (giống
        // Phiếu 3/4, xem IChuKyPhieuService.KhoiTaoLuongKyAsync) — thứ tự ký
        // (Nhà thầu trước, Phòng ban sau...) do Admin cấu hình BuocThuTu ở
        // màn "Luồng ký".
        Task<Phieu1KiemTra> GuiKyAsync(int id);

        // Đọc IChuKyPhieuService.TrangThaiTongAsync rồi cập nhật TrangThai của
        // chính phiếu này — gọi sau khi FE ký/từ chối thành công qua endpoint
        // chung /api/chu-ky-phieu/{id}/ky|tu-choi (xem LuongTrinhKy.md).
        Task<Phieu1KiemTra> DongBoTrangThaiAsync(int id);
    }
}
