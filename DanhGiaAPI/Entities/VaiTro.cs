namespace DanhGiaAPI.Entities
{
    public class VaiTro
    {
        public int Id { get; set; }
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public bool CoQuyenDuyetTk { get; set; }
    }
}
