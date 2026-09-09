namespace DanhGiaAPI.DTOs.LuongKy
{
    // Dùng cho dropdown "chỉ định người ký" / "ký thay" ở FE — chỉ trả thông
    // tin tối thiểu, không phải NguoiDungListItemDto đầy đủ.
    public class NguoiKyKhaDungDto
    {
        public int Id { get; set; }
        public string HoTen { get; set; } = null!;
        public string TenDangNhap { get; set; } = null!;
    }
}
