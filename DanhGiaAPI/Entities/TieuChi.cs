namespace DanhGiaAPI.Entities
{
    public class TieuChi
    {
        public int Id { get; set; }
        public int? NhomId { get; set; }
        public string NoiDung { get; set; } = null!;
        public int ThuTu { get; set; }
        public bool MacDinh { get; set; } = true;
        public bool DangHoatDong { get; set; } = true;
        // Chỉ dùng khi nhóm cha có LoaiPhieu = "PHIEU4" — mẫu để seed
        // Phieu4_Dong khi tạo phiếu mới (xem Phieu4Service.KhoiTaoDongTuMauAsync).
        // NULL/không dùng với Phiếu 1/2/3.
        public string? Dvt { get; set; }
        public string? LoaiDong { get; set; }   // NHAP_TAY, TINH_TRUNG_BINH, TINH_TY_LE...
        public string? CongThuc { get; set; }
    }
}
