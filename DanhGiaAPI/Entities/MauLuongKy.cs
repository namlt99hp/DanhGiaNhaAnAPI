namespace DanhGiaAPI.Entities
{
    public class MauLuongKy
    {
        public int Id { get; set; }
        public string LoaiPhieu { get; set; } = null!;
        public int BuocThuTu { get; set; }
        public string TenBuoc { get; set; } = null!;
        public string LoaiNguoiKy { get; set; } = null!; // PHONG_BAN / NHA_THAU / VAI_TRO
        public int? PhongBanId { get; set; }
        public int? VaiTroId { get; set; }
        public bool BatBuoc { get; set; } = true;
    }
}
