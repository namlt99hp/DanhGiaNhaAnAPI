namespace DanhGiaAPI.Entities
{
    public class Phieu1ChiTiet
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public int? NhomId { get; set; }
        public int? TieuChiId { get; set; } // NULL nếu là dòng tự thêm
        public string? NoiDungTuThem { get; set; }
        public string? KetQua { get; set; } // DAT / KHONG_DAT
        public string? GhiChu { get; set; } // HTML CKEditor
        public int ThuTu { get; set; }
    }
}
