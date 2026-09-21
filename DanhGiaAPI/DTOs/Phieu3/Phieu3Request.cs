using DanhGiaAPI.DTOs.Common;

namespace DanhGiaAPI.DTOs.Phieu3
{
    public class Phieu3Request
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int NhaThauId { get; set; }
        // Danh sách "đoạn" thời gian + địa điểm áp dụng — khai báo ngay lúc
        // lập phiếu, có thể thêm/bớt sau qua ThemDoanAsync/XoaDoanAsync (chỉ
        // khi phiếu còn Nháp/Từ chối). Thay thế hoàn toàn suy luận cũ.
        public List<DoanRequest> Doan { get; set; } = new();
    }
}
