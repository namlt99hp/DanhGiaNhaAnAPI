namespace DanhGiaAPI.DTOs.KetQuaDanhGiaPhanBo
{
    // Request cho cron gọi sau khi 1 bữa ăn kết thúc (VD Sáng kết thúc 8h ->
    // cron chạy 9h, Trưa kết thúc 13h -> cron chạy 14h — xem
    // 02. Phantich/modules/KetQuaDanhGiaPhanBo.md).
    public class KetQuaDanhGiaPhanBoTinhLaiRequest
    {
        // -> BuaAn.ID (01..04 = Sáng..Đêm).
        public int ID_BuaAn { get; set; }

        // Mặc định hôm nay (giờ server) nếu không truyền.
        public DateTime? Ngay { get; set; }
    }
}
