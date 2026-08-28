namespace DanhGiaAPI.Entities
{
    public class NhatKyChinhSua
    {
        public long Id { get; set; }
        public string LoaiDoiTuong { get; set; } = null!;
        public int DoiTuongId { get; set; }
        public string? TenTruong { get; set; }
        public string? GiaTriCu { get; set; }
        public string? GiaTriMoi { get; set; }
        public int? NguoiThayDoi { get; set; }
        public DateTime NgayThayDoi { get; set; } = DateTime.Now;
    }
}
