namespace DanhGiaAPI.DTOs.Phieu2
{
    public class Phieu2Request
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int NhaThauId { get; set; }
        public int? BepAnId { get; set; }
        // 1 phiếu có thể gộp nhiều nhà ăn của cùng bếp ăn (đánh giá 1 lần cho
        // nhiều nhà ăn) — chọn tự do, không ràng buộc theo BepAnId (xem
        // Phieu2Service.KiemTraNhaAnAsync).
        public List<int> NhaAnIds { get; set; } = new();
        public DateTime? ThoiGianTu { get; set; }
        public DateTime? ThoiGianDen { get; set; }
        public string? DiaDiem { get; set; }
        public string? ThoiGianKiemTraText { get; set; }
        public int? Phieu1Id { get; set; }
        public List<Phieu2TieuChiRequest> TieuChi { get; set; } = new();
    }
}
