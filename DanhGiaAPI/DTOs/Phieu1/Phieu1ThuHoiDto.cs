namespace DanhGiaAPI.DTOs.Phieu1
{
    // Dùng cho GET /api/phieu1/{id}/lich-su-thu-hoi — kèm họ tên người thu
    // hồi resolve sẵn (xem Phieu1Service.LichSuThuHoiAsync), để FE hiển thị
    // trực tiếp mà không cần tra thêm theo NguoiThuHoiId.
    public class Phieu1ThuHoiDto
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
