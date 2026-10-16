namespace DanhGiaAPI.DTOs.KetQuaDanhGiaLoaiTru
{
    // DanhSachAsync trả DTO này (thay entity thô) để kèm sẵn NguoiTaoTen —
    // tránh FE phải gọi thêm API /nguoi-dung (quyền QuanLyTaiKhoan, rộng hơn
    // cần thiết cho màn hình này) chỉ để resolve 1 cái tên.
    public class KetQuaDanhGiaLoaiTruDto
    {
        public int ID { get; set; }
        public DateTime Ngay { get; set; }
        public int ID_BuaAn { get; set; }
        public string? TenBuaAn { get; set; }
        public int DiaDiemNhaAnId { get; set; }
        public string? TenDiaDiem { get; set; }
        public string? LyDo { get; set; }
        public int NguoiTao { get; set; }
        public string? NguoiTaoTen { get; set; }
        public DateTime NgayTao { get; set; }
    }
}
