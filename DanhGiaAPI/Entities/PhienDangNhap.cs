namespace DanhGiaAPI.Entities
{
    public class PhienDangNhap
    {
        public int Id { get; set; }
        public int NguoiDungId { get; set; }
        public string MaToken { get; set; } = null!;
        public string? DiaChiIp { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime? NgayHetHan { get; set; }
    }
}
