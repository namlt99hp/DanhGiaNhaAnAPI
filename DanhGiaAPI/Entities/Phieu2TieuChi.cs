using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu2_TieuChi")]
    public class Phieu2TieuChi
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public string MaTieuChi { get; set; } = string.Empty;
        public string? TenTieuChi { get; set; }
        public bool Dat { get; set; }
        public bool KhongDat { get; set; }
        public decimal? Diem { get; set; }
        public string? GhiChu { get; set; }
        public int ThuTu { get; set; }
    }
}
