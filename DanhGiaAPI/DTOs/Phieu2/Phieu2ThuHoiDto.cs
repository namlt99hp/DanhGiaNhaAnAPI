namespace DanhGiaAPI.DTOs.Phieu2
{
    // Xem Phieu1ThuHoiDto.cs — cùng thiết kế, dùng cho
    // GET /api/phieu2/{id}/lich-su-thu-hoi.
    public class Phieu2ThuHoiDto
    {
        public int Id { get; set; }
        public int PhienBan { get; set; }
        public string TrangThaiTruocKhiThuHoi { get; set; } = null!;
        public int NguoiThuHoiId { get; set; }
        public string? NguoiThuHoiHoTen { get; set; }
        public DateTime NgayThuHoi { get; set; }
        public string LyDoThuHoi { get; set; } = null!;
    }
}
