namespace DanhGiaAPI.DTOs.Phieu1
{
    public class Phieu1ChiTietRequest
    {
        // null/0 = dòng mới; có giá trị = cập nhật dòng đã tồn tại (giữ nguyên Id
        // để không làm "mồ côi" ảnh minh chứng đã đính kèm ở TepDinhKem).
        public int? Id { get; set; }

        public int? NhomId { get; set; }
        public int? TieuChiId { get; set; } // NULL nếu là dòng tự thêm
        public string? NoiDungTuThem { get; set; }
        public string? KetQua { get; set; } // DAT / KHONG_DAT
        public string? GhiChu { get; set; }
        public int ThuTu { get; set; }
    }
}
