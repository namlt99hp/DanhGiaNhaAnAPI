namespace DanhGiaAPI.DTOs.Phieu4
{
    public class Phieu4GiaTriItem
    {
        public int DongId { get; set; }
        public int NhaThauId { get; set; }
        public decimal? GiaTri { get; set; }
    }

    // Sửa tay 1 ô "giá trị chung" (Bảng 4/5 — không chia theo cột nhà thầu),
    // xem Phieu4Dong.GiaTriChung.
    public class Phieu4GiaTriChungItem
    {
        public int DongId { get; set; }
        public decimal? GiaTriChung { get; set; }
    }

    // Sửa tay 1 hoặc nhiều ô cùng lúc — dùng chung cho mọi bảng (kể cả dòng
    // "Tổng số suất ăn" ở Bảng 1, và toàn bộ Bảng 2-3 qua GiaTri; Bảng 4-5 qua
    // GiaTriChung).
    public class Phieu4CapNhatGiaTriRequest
    {
        public List<Phieu4GiaTriItem> GiaTri { get; set; } = new();
        public List<Phieu4GiaTriChungItem> GiaTriChung { get; set; } = new();
    }
}
