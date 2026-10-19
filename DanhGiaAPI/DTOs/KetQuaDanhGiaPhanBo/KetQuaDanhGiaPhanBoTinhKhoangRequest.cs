namespace DanhGiaAPI.DTOs.KetQuaDanhGiaPhanBo
{
    // Request cho POST /api/KetQuaDanhGiaPhanBo/tinh-lai-khoang — nút "Phân
    // bổ lại" ở KetQuaDanhGiaPhanBoPageV2 (gọi tay theo đúng bộ lọc đang
    // chọn trên màn hình search). BE giới hạn (DenNgay - TuNgay) tối đa 2
    // ngày (xem KetQuaDanhGiaPhanBoController) vì mỗi ngày quét hết các bữa
    // x địa điểm đang active — khoảng dài dễ quá tải DB.
    public class KetQuaDanhGiaPhanBoTinhKhoangRequest
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }

        // null = tất cả 4 bữa thật (Sáng..Đêm).
        public int? ID_BuaAn { get; set; }

        // null = tất cả địa điểm đang IsActive.
        public int? DiaDiemNhaAnId { get; set; }
    }
}
