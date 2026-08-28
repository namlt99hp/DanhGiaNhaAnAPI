namespace DanhGiaAPI.Entities
{
    public class TepDinhKem
    {
        public int Id { get; set; }
        public string LoaiDoiTuong { get; set; } = null!;
        public int? DoiTuongId { get; set; }
        public string DuongDanTep { get; set; } = null!;
        public string? TenTep { get; set; }
        public int? NguoiTaiLen { get; set; }
        public DateTime NgayTaiLen { get; set; } = DateTime.Now;
    }
}
