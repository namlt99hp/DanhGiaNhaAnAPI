namespace DanhGiaAPI.DTOs.Common
{
    // Kết quả phân trang dùng chung cho các màn danh sách Phiếu 1-4 (tìm kiếm
    // theo số hiệu/khoảng ngày/phiếu của tôi lập + phân trang server-side) —
    // xem Phieu1..4Service.DanhSachAsync.
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
