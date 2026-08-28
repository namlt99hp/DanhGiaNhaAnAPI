namespace DanhGiaAPI.DTOs.Phieu2
{
    public class Phieu2TieuChiRequest
    {
        public int? Id { get; set; }
        public string MaTieuChi { get; set; } = string.Empty;
        public string? TenTieuChi { get; set; }
        public bool Dat { get; set; }
        public bool KhongDat { get; set; }
        public decimal? Diem { get; set; }
        public string? GhiChu { get; set; }
        public int ThuTu { get; set; }
    }
}
