using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    // "Đoạn" thời gian nhà thầu phụ trách 1 tập địa điểm ăn (xem
    // Phieu3DoanDiaDiem) — thay thế hoàn toàn suy luận "nhà ăn rõ ràng" cũ
    // (Phieu3Service.LayNhaAnRoRangAsync, đã xóa). Biên (Ngày, Bữa ăn) — cả 2
    // đầu ĐỀU BAO GỒM bữa được chọn (xác nhận nghiệp vụ). Không FK constraint
    // tới PhieuId/BuaAnId (đúng convention toàn module — xem
    // 02. Phantich/schema_module_danh_gia_nha_an.sql).
    [Table("Phieu3_Doan")]
    public class Phieu3Doan
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public DateTime TuNgay { get; set; }
        public int TuBuaAnId { get; set; }
        public DateTime DenNgay { get; set; }
        public int DenBuaAnId { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
