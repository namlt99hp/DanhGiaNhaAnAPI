using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu3_YKienNhaThau")]
    public class Phieu3YKienNhaThau
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public string? YKien { get; set; }
    }
}
