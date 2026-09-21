namespace DanhGiaAPI.DTOs.Common
{
    // "Đoạn" thời gian — khung (Ngày, Bữa ăn) bắt đầu -> (Ngày, Bữa ăn) kết
    // thúc (CẢ 2 ĐẦU đều bao gồm bữa được chọn) + danh sách địa điểm ăn áp
    // dụng trong đoạn — dùng chung cho Phiếu 3 (đoạn gắn trực tiếp vào phiếu)
    // và Phiếu 4 (đoạn gắn vào từng cột nhà thầu). Xem Phieu3Doan/Phieu4Doan.
    public class DoanRequest
    {
        public DateTime TuNgay { get; set; }
        public int TuBuaAnId { get; set; }
        public DateTime DenNgay { get; set; }
        public int DenBuaAnId { get; set; }
        public List<int> DiaDiemNhaAnIds { get; set; } = new();
    }
}
