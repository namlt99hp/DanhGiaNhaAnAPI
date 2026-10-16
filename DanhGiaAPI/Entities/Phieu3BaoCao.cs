using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu3_BaoCao")]
    public class Phieu3BaoCao
    {
        public int Id { get; set; }
        public string SoHieu { get; set; } = string.Empty;
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int NhaThauId { get; set; }
        // Snapshot NhaThau.SoHopDong tại thời điểm LẬP báo cáo (xem
        // Phieu3Service.ThemAsync) — sửa số hợp đồng ở danh mục Nhà thầu sau
        // đó KHÔNG làm thay đổi các báo cáo đã lập.
        public string? SoHopDong { get; set; }
        public int? NguoiTao { get; set; }
        public string TrangThai { get; set; } = "NHAP";
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
