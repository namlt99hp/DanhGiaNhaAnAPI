namespace DanhGiaAPI.Entities
{
    public class BoDemSoHieu
    {
        public int Id { get; set; }
        public string LoaiPhieu { get; set; } = null!;
        public string? PhamVi { get; set; }
        public int Nam { get; set; }
        public int? Thang { get; set; }
        public int SoThuTuCuoi { get; set; }
    }
}
