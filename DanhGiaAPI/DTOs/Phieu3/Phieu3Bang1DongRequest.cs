namespace DanhGiaAPI.DTOs.Phieu3
{
    public class Phieu3Bang1DongRequest
    {
        // LUOT_CBNV_THAM_GIA, TONG_SUAT_AN, TY_LE_PHAN_TRAM
        public string MaDong { get; set; } = string.Empty;
        public decimal? Diem1 { get; set; }
        public decimal? Diem2 { get; set; }
        public decimal? Diem3 { get; set; }
        public decimal? Diem4 { get; set; }
        public decimal? Diem5 { get; set; }
        public decimal? Tong { get; set; }
    }
}
