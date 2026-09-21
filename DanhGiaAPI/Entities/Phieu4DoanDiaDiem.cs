using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu4_DoanDiaDiem")]
    public class Phieu4DoanDiaDiem
    {
        public int Id { get; set; }
        public int DoanId { get; set; }
        // -> dbo.DiaDiemNhaAn.ID, liên kết LOGIC, không FK constraint thật.
        public int DiaDiemNhaAnId { get; set; }
    }
}
