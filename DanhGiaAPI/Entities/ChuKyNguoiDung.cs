namespace DanhGiaAPI.Entities
{
    public class ChuKyNguoiDung
    {
        public int Id { get; set; }
        public int NguoiDungId { get; set; }
        public string DuongDanChuKy { get; set; } = null!;
        public bool DangSuDung { get; set; } = true;
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
