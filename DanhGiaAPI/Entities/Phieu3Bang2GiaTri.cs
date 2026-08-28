using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu3_Bang2GiaTri")]
    public class Phieu3Bang2GiaTri
    {
        public int Id { get; set; }
        public int DongId { get; set; }
        // TC1..TC6 — nội dung/công thức chưa xác nhận nghiệp vụ, nhập tay hoàn toàn
        // (xem 02. Phantich/modules/Phieu3_BaoCaoThang.md)
        public string MaTieuChi { get; set; } = string.Empty;
        public decimal? GiaTri { get; set; }
        public bool ChinhSuaThuCong { get; set; }
        public string? ThamChieuNguon { get; set; }
    }
}
