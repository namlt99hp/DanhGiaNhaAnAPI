namespace DanhGiaAPI.Entities
{
    public class NhomTieuChi
    {
        public int Id { get; set; }
        public string LoaiPhieu { get; set; } = null!;
        public string? Ma { get; set; }
        public string Ten { get; set; } = null!;
        public int ThuTu { get; set; }
        public bool DangHoatDong { get; set; } = true;
        // Chỉ có ý nghĩa khi LoaiPhieu = "PHIEU4": nhóm dòng này thuộc Bảng mấy
        // (2..5 — Bảng 1 có cấu trúc cố định, không seed từ master). NULL với
        // các loại phiếu khác (Phiếu 1/2/3, vốn chỉ có 1 danh sách nhóm phẳng).
        public int? SoBang { get; set; }
    }
}
