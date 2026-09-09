namespace DanhGiaAPI.Entities
{
    // Bảng n-n VaiTro <-> Quyen, khóa ghép (xem AppDbContext.OnModelCreating) —
    // cùng kiểu với NguoiDungVaiTro.
    public class VaiTroQuyen
    {
        public int VaiTroId { get; set; }
        public int QuyenId { get; set; }
    }
}
