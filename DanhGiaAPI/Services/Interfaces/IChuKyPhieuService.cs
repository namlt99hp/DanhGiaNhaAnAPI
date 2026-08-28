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
        Task<List<ChuKyPhieu>> TienDoKyAsync(string loaiPhieu, int doiTuongId);

        // CHUA_KHOI_TAO | CHO_KY | DA_DUYET | TU_CHOI — Service của Phiếu tự đọc
        // giá trị này để đồng bộ cột TrangThai của bảng phiếu chính (không có
        // cơ chế tự động 2 chiều, vì mỗi Phiếu có bảng riêng).
        Task<string> TrangThaiTongAsync(string loaiPhieu, int doiTuongId);

        Task<ChuKyPhieu> KyAsync(int id, int nguoiKyId, int? chuKyId, string? ghiChu);
        Task<ChuKyPhieu> TuChoiAsync(int id, int nguoiKyId, string ghiChu);
    }
}
