namespace DanhGiaAPI.DTOs.KetQuaDanhGiaPhanBo
{
    // Dùng cho GET /api/KetQuaDanhGiaPhanBo — giống hệt KetQuaDanhGiaPhanBo
    // (Models/KetQuaDanhGiaPhanBo.cs) nhưng thêm TenBuaAn (join BuaAn theo
    // ID_BuaAn) để FE không phải tự tra cứu tên bữa ăn qua API BuaAn riêng.
    public class KetQuaDanhGiaPhanBoDanhSachItemDto
    {
        public int ID { get; set; }
        public DateTime Ngay { get; set; }
        public int ID_BuaAn { get; set; }
        public string TenBuaAn { get; set; } = string.Empty;
        public int DiaDiemNhaAnId { get; set; }

        public int Diem1Goc { get; set; }
        public int Diem2Goc { get; set; }
        public int Diem3Goc { get; set; }
        public int Diem4Goc { get; set; }
        public int Diem5Goc { get; set; }
        public int TongGoc { get; set; }

        public int? ComThucTe { get; set; }

        public int Diem1 { get; set; }
        public int Diem2 { get; set; }
        public int Diem3 { get; set; }
        public int Diem4 { get; set; }
        public int Diem5 { get; set; }
        public int Tong { get; set; }

        public DateTime ThoiGianTinh { get; set; }
    }
}
