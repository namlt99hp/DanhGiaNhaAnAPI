namespace DanhGiaAPI.Entities
{
    public class Phieu1ChiTiet
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public int? NhomId { get; set; }
        public int? TieuChiId { get; set; } // NULL nếu là dòng tự thêm
        // Snapshot NỘI DUNG tiêu chí tại thời điểm LƯU (ghi 1 lần, không tự
        // cập nhật lại) — để phiếu cũ (đặc biệt đã ký) hiển thị đúng nguyên
        // văn lúc ký, không đổi theo nếu Admin sửa/xóa tiêu chí gốc sau này.
        // Thêm 2026-09-01, xem 02. Phantich/modules/Phieu1_KiemTraVSATTP.md.
        public string? TenTieuChi { get; set; }
        public string? NoiDungTuThem { get; set; }
        public string? KetQua { get; set; } // DAT / KHONG_DAT
        public string? GhiChu { get; set; } // HTML CKEditor
        public int ThuTu { get; set; }
    }
}
