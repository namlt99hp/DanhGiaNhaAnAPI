using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu3_Bang2Dong")]
    public class Phieu3Bang2Dong
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public int PhongBanId { get; set; }
    }
}
