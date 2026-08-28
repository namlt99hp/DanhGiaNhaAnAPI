namespace DanhGiaAPI.DTOs.Phieu2
{
    public class Phieu2Request
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int NhaThauId { get; set; }
        public int? BepAnId { get; set; }
        public int NhaAnId { get; set; }
        public DateTime? ThoiGianTu { get; set; }
        public DateTime? ThoiGianDen { get; set; }
        public string? DiaDiem { get; set; }
        public string? ThoiGianKiemTraText { get; set; }
        public int? Phieu1Id { get; set; }
        public List<Phieu2TieuChiRequest> TieuChi { get; set; } = new();
    }
}
