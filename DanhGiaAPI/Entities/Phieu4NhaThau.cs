using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu4_NhaThau")]
    public class Phieu4NhaThau
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public int NhaThauId { get; set; }
        public int ThuTu { get; set; }
    }
}
