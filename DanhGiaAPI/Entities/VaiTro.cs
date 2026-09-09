namespace DanhGiaAPI.Entities
{
    public class VaiTro
    {
        public int Id { get; set; }
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;

        // Bypass TẤT CẢ các policy quyền (xem CoQuyen() trong Program.cs) —
        // thay cho CoQuyenDuyetTk cũ (chỉ bypass đúng 1 policy). Vai trò nào
        // cần "làm được mọi thứ" (VD ADMIN) thì bật cờ này thay vì phải gán đủ
        // từng dòng Quyen. Xem 02. Phantich/modules/VaiTro.md.
        public bool LaQuanTriVien { get; set; }
    }
}
