namespace DanhGiaAPI.DTOs.KetQuaDanhGiaPhanBo
{
    // Kết quả trả về cho FE sau khi bấm "Phân bổ lại" — tóm tắt số tổ hợp
    // (Ngay, IdBuaAn) đã xử lý/bỏ qua, để hiện thông báo cho người dùng.
    public class KetQuaDanhGiaPhanBoTinhKhoangResultDto
    {
        // Số tổ hợp (Ngay, IdBuaAn) đã gọi tính lại.
        public int SoToHopDaTinh { get; set; }

        // Số tổ hợp bị bỏ qua vì đã tính đủ từ trước (ngày đã qua, không
        // phải hôm nay) — xem TinhLaiTheoKhoangAsync.
        public int SoToHopBoQua { get; set; }

        // Tổng số dòng KetQuaDanhGiaPhanBo được ghi mới/ghi đè (cộng dồn từ
        // mọi tổ hợp đã tính).
        public int TongDongGhi { get; set; }
    }
}
