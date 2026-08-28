namespace DanhGiaAPI.Entities
{
    public class PhongBan
    {
        public int Id { get; set; }
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public bool DangHoatDong { get; set; }
    }
}
