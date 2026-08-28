namespace DanhGiaAPI.DTOs.Auth
{
    public class NguoiDungInfoDto
    {
        public int Id { get; set; }
        public string TenDangNhap { get; set; } = null!;
        public string HoTen { get; set; } = null!;
        public string? Email { get; set; }
        public int? PhongBanId { get; set; }
        public int? NhaThauId { get; set; }
        public string TrangThai { get; set; } = null!;
        public List<string> DanhSachVaiTro { get; set; } = new();
    }
}
