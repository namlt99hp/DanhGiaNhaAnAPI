namespace DanhGiaAPI.DTOs.Phieu3
{
    // Bọc thêm thông tin "sửa tay theo từng ô" (từ NhatKyChinhSua) lên trên
    // Phieu3Bang1Dong — ChinhSuaThuCong của entity là cờ CẤP DÒNG (khoá không
    // cho "tính lại" ghi đè cả dòng), còn TruongDaSuaTay ở đây là danh sách
    // CÁC Ô cụ thể (diem1..diem5, tong) người dùng từng sửa tay, kèm giá trị
    // hệ thống gốc của từng ô đó — phục vụ tô vàng + tooltip so sánh trên UI,
    // xem ChiTietAsync (Phieu3Service).
    public class Phieu3Bang1DongDto
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public string MaDong { get; set; } = string.Empty;
        public string? TenDong { get; set; }
        public decimal? Diem1 { get; set; }
        public decimal? Diem2 { get; set; }
        public decimal? Diem3 { get; set; }
        public decimal? Diem4 { get; set; }
        public decimal? Diem5 { get; set; }
        public decimal? Tong { get; set; }
        public bool ChinhSuaThuCong { get; set; }
        public string? NguonDuLieu { get; set; }

        // "diem1".."diem5", "tong" — các ô đã từng bị sửa tay (khớp key cột bên UI)
        public List<string> TruongDaSuaTay { get; set; } = new();
        public decimal? Diem1HeThong { get; set; }
        public decimal? Diem2HeThong { get; set; }
        public decimal? Diem3HeThong { get; set; }
        public decimal? Diem4HeThong { get; set; }
        public decimal? Diem5HeThong { get; set; }
        public decimal? TongHeThong { get; set; }
    }
}
