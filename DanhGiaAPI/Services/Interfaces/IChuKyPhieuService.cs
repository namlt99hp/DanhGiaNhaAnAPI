using DanhGiaAPI.DTOs.LuongKy;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IChuKyPhieuService
    {
        // Gọi từ Service của từng Phiếu khi chuyển NHAP -> CHO_KY: tạo các dòng
        // ChuKyPhieu (CHO_KY) từ MauLuongKy đang cấu hình cho loaiPhieu đó.
        Task KhoiTaoLuongKyAsync(string loaiPhieu, int doiTuongId);

        // Tiến độ ký hiện tại (1 dòng mới nhất / BuocThuTu — nếu phiếu bị từ chối
        // rồi khởi tạo lại, các dòng cũ của lượt trước vẫn còn trong DB để audit).
        // Trả DTO (không phải entity) vì có resolve thêm DuongDanChuKy để FE
        // render ảnh chữ ký — xem DTOs/LuongKy/ChuKyPhieuDto.cs.
        Task<List<ChuKyPhieuDto>> TienDoKyAsync(string loaiPhieu, int doiTuongId);

        // CHUA_KHOI_TAO | CHO_KY | DA_DUYET | TU_CHOI — Service của Phiếu tự đọc
        // giá trị này để đồng bộ cột TrangThai của bảng phiếu chính (không có
        // cơ chế tự động 2 chiều, vì mỗi Phiếu có bảng riêng).
        Task<string> TrangThaiTongAsync(string loaiPhieu, int doiTuongId);

        // nguoiKyThayId: tùy chọn — ký thay cho người khác (phải cùng đủ điều
        // kiện ký bước này). NULL = ký cho chính nguoiKyId (mặc định).
        Task<ChuKyPhieu> KyAsync(int id, int nguoiKyId, int? chuKyId, string? ghiChu, int? nguoiKyThayId);
        Task<ChuKyPhieu> TuChoiAsync(int id, int nguoiKyId, string ghiChu);

        // Chỉ định/đổi người ký dự kiến cho 1 bước còn CHO_KY — cả người thực
        // hiện (nguoiThucHienId) lẫn người được chỉ định (nguoiKyDuKienId) đều
        // phải tự đủ điều kiện ký đúng bước đó (xem VaiTro.md/LuongTrinhKy.md).
        Task<ChuKyPhieu> DatNguoiKyDuKienAsync(int id, int nguoiThucHienId, int nguoiKyDuKienId);

        // Danh sách người đủ điều kiện ký 1 bước cụ thể (dùng cho dropdown FE
        // "chỉ định người ký" / "ký thay").
        Task<List<NguoiKyKhaDungDto>> DanhSachNguoiKyKhaDungAsync(int id);
    }
}
