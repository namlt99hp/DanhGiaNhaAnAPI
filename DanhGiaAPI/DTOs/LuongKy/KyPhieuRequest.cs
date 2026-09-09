namespace DanhGiaAPI.DTOs.LuongKy
{
    public class KyPhieuRequest
    {
        public int? ChuKyId { get; set; }
        public string? GhiChu { get; set; }

        // Tùy chọn — "ký thay" cho người khác (phải cùng đủ điều kiện ký bước
        // này như người đang đăng nhập). Để trống = ký cho chính mình (mặc định).
        public int? NguoiKyThayId { get; set; }
    }
}
