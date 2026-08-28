using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu4_TongHop")]
    public class Phieu4TongHop
    {
        public int Id { get; set; }
        public string SoHieu { get; set; } = string.Empty;
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public int? NguoiTao { get; set; }
        public string TrangThai { get; set; } = "NHAP";
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
