namespace DanhGiaAPI.DTOs.Common
{
    public class DoanDto
    {
        public int Id { get; set; }
        public DateTime TuNgay { get; set; }
        public int TuBuaAnId { get; set; }
        public string? TuBuaAnCode { get; set; }
        public DateTime DenNgay { get; set; }
        public int DenBuaAnId { get; set; }
        public string? DenBuaAnCode { get; set; }
        public List<int> DiaDiemNhaAnIds { get; set; } = new();
    }
}
