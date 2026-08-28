using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu4_GiaTri")]
    public class Phieu4GiaTri
    {
        public int Id { get; set; }
        public int DongId { get; set; }
        public int NhaThauId { get; set; }
        public decimal? GiaTri { get; set; }
        public bool ChinhSuaThuCong { get; set; }
    }
}
