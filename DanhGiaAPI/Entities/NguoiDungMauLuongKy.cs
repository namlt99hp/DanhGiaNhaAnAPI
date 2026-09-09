namespace DanhGiaAPI.Entities
{
    // Gán TRỰC TIẾP 1 NguoiDung vào 1 dòng MauLuongKy cụ thể — thay thế hoàn
    // toàn cơ chế "LoaiNguoiKy = VAI_TRO" cũ (không còn đi qua bảng VaiTro).
    // Với PHONG_BAN/NHA_THAU, đây là lớp kiểm soát THỨ 2 cộng thêm vào việc
    // khớp PhongBanId/NhaThauId (không còn "cùng phòng ban/nhà thầu là tự
    // động ký được" nữa) — xem ChuKyPhieuService.KiemTraQuyenKyAsync và
    // 02. Phantich/modules/VaiTro.md mục 9.
    public class NguoiDungMauLuongKy
    {
        public int NguoiDungId { get; set; }
        public int MauLuongKyId { get; set; }
    }
}
