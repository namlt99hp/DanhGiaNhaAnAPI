using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    // "Đoạn" thời gian của 1 CỘT nhà thầu (Phieu4NhaThau) trong Phiếu 4 — mỗi
    // cột có bộ đoạn riêng, tương đương 1 "Phiếu 3 con". Thay thế hoàn toàn
    // suy luận "địa điểm rõ ràng" cũ (Phieu4Service.LayDiaDiemRoRangCuaNhaThauAsync,
    // đã xóa). Không FK constraint (đúng convention toàn module).
    [Table("Phieu4_Doan")]
    public class Phieu4Doan
    {
        public int Id { get; set; }
        public int NhaThauCotId { get; set; } // -> Phieu4_NhaThau.Id
        public DateTime TuNgay { get; set; }
        public int TuBuaAnId { get; set; }
        public DateTime DenNgay { get; set; }
        public int DenBuaAnId { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
