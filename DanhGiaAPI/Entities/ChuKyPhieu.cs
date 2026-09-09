namespace DanhGiaAPI.Entities
{
    public class ChuKyPhieu
    {
        public int Id { get; set; }
        public string LoaiDoiTuong { get; set; } = null!; // PHIEU1, PHIEU2, PHIEU3, PHIEU4
        public int DoiTuongId { get; set; }
        public int BuocThuTu { get; set; }
        public string? TenBuoc { get; set; }
        public int? NguoiKyId { get; set; }

        // Chỉ định trước (tùy chọn) — khi có giá trị, CHỈ đúng người này được
        // ghi nhận là người ký bước này (ràng buộc cứng, xem
        // ChuKyPhieuService.KyAsync/TuChoiAsync) — cho phép người khác cùng đủ
        // điều kiện "ký thay" (NguoiKyId cuối cùng vẫn phải khớp cột này) thay
        // vì bắt đúng người đó phải tự đăng nhập. Đặt/đổi được bất kỳ lúc nào
        // khi bước còn CHO_KY. Xem 02. Phantich/modules/LuongTrinhKy.md.
        public int? NguoiKyDuKienId { get; set; }

        public int? ChuKyId { get; set; }
        public string TrangThai { get; set; } = "CHO_KY"; // CHO_KY, DA_DUYET, TU_CHOI
        public string? GhiChu { get; set; }
        public DateTime? NgayKy { get; set; }
        // Số lượt ký, tăng dần mỗi lần KhoiTaoLuongKyAsync được gọi lại (gửi ký
        // lại sau khi bị từ chối) — phân biệt "nhiều người ký song song cùng 1
        // BuocThuTu" (cùng LuotKy) với "các lượt ký cũ đã bị từ chối" (LuotKy nhỏ
        // hơn). Xem LuongTrinhKy.md.
        public int LuotKy { get; set; } = 1;
    }
}
