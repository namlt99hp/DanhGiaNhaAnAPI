namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    public class ChuKyResponseDto
    {
        public int Id { get; set; }
        public string DuongDanChuKy { get; set; } = null!;
        public bool DangSuDung { get; set; }
        public DateTime NgayTao { get; set; }
    }
}
