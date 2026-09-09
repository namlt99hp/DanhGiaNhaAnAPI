namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    public class VaiTroResponseDto
    {
        public int Id { get; set; }
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public bool LaQuanTriVien { get; set; }
        public List<int> QuyenIds { get; set; } = new();
    }
}
