using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu2_YKienNhaThau")]
    public class Phieu2YKienNhaThau
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public string? YKien { get; set; }
        public string? NguoiPhanHoi { get; set; }
        public DateTime? NgayPhanHoi { get; set; }
    }
}
