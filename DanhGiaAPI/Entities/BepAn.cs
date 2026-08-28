namespace DanhGiaAPI.Entities
{
    public class BepAn
    {
        public int Id { get; set; }
        public int? NhaThauId { get; set; }
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public string? ViTri { get; set; }
        public string TrangThai { get; set; } = "HOAT_DONG";
    }
}
