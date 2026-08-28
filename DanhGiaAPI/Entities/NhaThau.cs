namespace DanhGiaAPI.Entities
{
    public class NhaThau
    {
        public int Id { get; set; }
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public DateTime? NgayBatDauHd { get; set; }
        public DateTime? NgayKetThucHd { get; set; }
        public string TrangThai { get; set; } = "HOAT_DONG";
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
