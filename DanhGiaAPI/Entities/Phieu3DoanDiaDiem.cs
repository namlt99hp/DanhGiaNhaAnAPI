using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu3_DoanDiaDiem")]
    public class Phieu3DoanDiaDiem
    {
        public int Id { get; set; }
        public int DoanId { get; set; }
        // -> dbo.DiaDiemNhaAn.ID, liên kết LOGIC, không FK constraint thật
        // (giống Phieu4Dong.DiaDiemNhaAnId).
        public int DiaDiemNhaAnId { get; set; }
    }
}
