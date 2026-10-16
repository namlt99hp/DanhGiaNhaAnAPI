namespace DanhGiaAPI.Entities
{
    // Lịch sử thu hồi Phiếu 1 — ghi lại mỗi lần người có quyền "Đánh giá/nhập
    // liệu" (đúng phòng ban đã lập phiếu) hoặc Admin đưa phiếu đang CHO_KY/
    // DA_DUYET quay lại NHAP để sửa (VD phát hiện sai sót sau khi đã gửi ký).
    // Xem Phieu1Service.ThuHoiAsync + 02. Phantich/modules/Phieu1_KiemTraVSATTP.md.
    //
    // KHÔNG đụng ChuKyPhieu khi thu hồi — lượt ký (LuotKy) cũ giữ nguyên làm
    // lịch sử; lần "Gửi ký" lại sau khi sửa sẽ tự tạo LuotKy MỚI (xem
    // ChuKyPhieuService.KhoiTaoLuongKyAsync), tương đương "ký lại từ đầu".
    public class Phieu1ThuHoi
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }

        // Thứ tự lần thu hồi của CHÍNH phiếu này (1, 2, 3...) — "version thu
        // hồi" để phân biệt các lần thu hồi khác nhau khi rà soát.
        public int PhienBan { get; set; }

        // "CHO_KY" hoặc "DA_DUYET" — trạng thái phiếu NGAY TRƯỚC khi bị thu
        // hồi, để biết thu hồi từ bước nào (VD thu hồi khi đã ký duyệt xong
        // là nghiêm trọng hơn khi mới đang chờ ký).
        public string TrangThaiTruocKhiThuHoi { get; set; } = null!;

        public int NguoiThuHoiId { get; set; } // -> NguoiDung.Id, không FK (claim "sub")
        public DateTime NgayThuHoi { get; set; } = DateTime.Now;
        public string LyDoThuHoi { get; set; } = null!;
    }
}
