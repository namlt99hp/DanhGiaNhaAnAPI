namespace DanhGiaAPI.Models
{
    // Đánh dấu 1 (Ngày, Bữa ăn, Địa điểm) là "máy kiosk hỏng — không thu được
    // đánh giá thật" (xem 02. Phantich/modules/KetQuaDanhGiaPhanBo.md, mục
    // "Loại trừ do lỗi thiết bị"). Chỉ ảnh hưởng "Lượt đánh giá"
    // (LUOT_CBNV_THAM_GIA/nhóm 2) của Phieu3Service/Phieu4Service — KHÔNG ảnh
    // hưởng "Tổng suất ăn" (vẫn tính bình thường từ DuLieuCom, xác nhận
    // nghiệp vụ 2026-10-13).
    public class KetQuaDanhGiaLoaiTru
    {
        public int ID { get; set; }
        public DateTime Ngay { get; set; }
        public int ID_BuaAn { get; set; }
        public int DiaDiemNhaAnId { get; set; }
        public string? LyDo { get; set; }

        // Truy vết ai đánh dấu, lúc nào — claim "sub" (ClaimsPrincipalExtensions.GetNguoiDungId).
        public int NguoiTao { get; set; }
        public DateTime NgayTao { get; set; }
    }
}
