using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    // Liên kết N-N Phiếu 2 <-> Nhà ăn (DiaDiemNhaAn) — 1 phiếu đánh giá có thể
    // gộp nhiều nhà ăn của cùng 1 bếp ăn (đánh giá 1 lần cho nhiều nhà ăn).
    // Không có FK constraint thật (giống cách Phieu2DanhGia tham chiếu
    // DiaDiemNhaAn từ trước — chọn tự do, không ràng buộc), xem
    // Phieu2_DanhGiaSuatAn.md.
    [Table("Phieu2_NhaAn")]
    public class Phieu2NhaAn
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public int NhaAnId { get; set; }
    }
}
