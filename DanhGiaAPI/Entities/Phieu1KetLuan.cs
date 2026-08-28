namespace DanhGiaAPI.Entities
{
    public class Phieu1KetLuan
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public int? SoLuongDat { get; set; }
        public int? TongSoTieuChi { get; set; }
        public decimal? TyLePhanTram { get; set; }
        public string? KetLuan { get; set; } // DAT / KHONG_DAT
        public decimal? DiemDanhGia { get; set; }
        public string? GhiChu { get; set; } // HTML CKEditor
    }
}
