using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu3_Bang1Dong")]
    public class Phieu3Bang1Dong
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        // LUOT_CBNV_THAM_GIA, TONG_SUAT_AN, TY_LE_PHAN_TRAM
        public string MaDong { get; set; } = string.Empty;
        public string? TenDong { get; set; }
        public decimal? Diem1 { get; set; }
        public decimal? Diem2 { get; set; }
        public decimal? Diem3 { get; set; }
        public decimal? Diem4 { get; set; }
        public decimal? Diem5 { get; set; }
        public decimal? Tong { get; set; }
        public bool ChinhSuaThuCong { get; set; }
        public string? NguonDuLieu { get; set; }
    }
}
