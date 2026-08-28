using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu4_Bang")]
    public class Phieu4Bang
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public int SoBang { get; set; }
        public string? TenBang { get; set; }
    }
}
