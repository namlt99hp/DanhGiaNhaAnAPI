namespace DanhGiaAPI.DTOs.Phieu4
{
    public class Phieu4Request
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public List<int> NhaThauIds { get; set; } = new();
    }
}
