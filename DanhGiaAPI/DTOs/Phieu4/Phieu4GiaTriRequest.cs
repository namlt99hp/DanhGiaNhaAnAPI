namespace DanhGiaAPI.DTOs.Phieu4
{
    public class Phieu4GiaTriItem
    {
        public int DongId { get; set; }
        public int NhaThauId { get; set; }
        public decimal? GiaTri { get; set; }
    }

    // Sửa tay 1 hoặc nhiều ô cùng lúc — dùng chung cho mọi bảng (kể cả dòng
    // "Tổng số suất ăn" ở Bảng 1, và toàn bộ Bảng 2-5).
    public class Phieu4CapNhatGiaTriRequest
    {
        public List<Phieu4GiaTriItem> GiaTri { get; set; } = new();
    }
}
