
namespace DanhGiaAPI.Models
{
    public class KhungGioDanhGia
    {
        public int ID { get; set; }
        public string CaAn { get; set; }
        public TimeSpan TuGio { get; set; }
        public TimeSpan DenGio { get; set; }
        public string? MoTa { get; set; }
        public string? DiaDiem { get; set; }
    }
}
