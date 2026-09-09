namespace DanhGiaAPI.Entities
{
    // Danh mục quyền cố định (mỗi Ma luôn khớp 1 policy trong Program.cs) — xem
    // 02. Phantich/modules/VaiTro.md. Không có API tạo/sửa/xóa qua UI vì thêm 1
    // quyền mới luôn cần thêm code (policy + gắn vào Controller tương ứng).
    public class Quyen
    {
        public int Id { get; set; }
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public string? NhomQuyen { get; set; }
    }
}
