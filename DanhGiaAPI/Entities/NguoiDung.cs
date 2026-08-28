namespace DanhGiaAPI.Entities
{
    public class NguoiDung
    {
        public int Id { get; set; }
        public string TenDangNhap { get; set; } = null!;
        public string MatKhauMaHoa { get; set; } = null!;
        public string HoTen { get; set; } = null!;
        public string? Email { get; set; }
        public string? SoDienThoai { get; set; }
        public int? PhongBanId { get; set; }
        public int? NhaThauId { get; set; }
        public string TrangThai { get; set; } = "CHO_DUYET";
        public int? NguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
