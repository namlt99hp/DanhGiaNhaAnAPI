using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu2_KetQua")]
    public class Phieu2KetQua
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public int? SoTieuChiDat { get; set; }
        public int TongSoTieuChi { get; set; } = 5;
    }
}
