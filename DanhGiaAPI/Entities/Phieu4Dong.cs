using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    [Table("Phieu4_Dong")]
    public class Phieu4Dong
    {
        public int Id { get; set; }
        public int BangId { get; set; }
        // Chỉ có ý nghĩa với Bảng 1 (nhóm 1..4 cố định, xem Phieu4Service).
        // Bảng 2-5 (nội dung tự do, chưa xác nhận nghiệp vụ) không dùng NhomSo.
        public int? NhomSo { get; set; }
        public int? Stt { get; set; }
        public string? NoiDung { get; set; }
        public string? Dvt { get; set; }
        // NHAP_TAY, DEM_TU_PHIEU2, TINH_TRUNG_BINH, TINH_TY_LE — xem Phieu4Service
        public string? LoaiDong { get; set; }
        public string? CongThuc { get; set; }
        // Dòng này được seed tự động từ TieuChi (master) nào khi tạo phiếu —
        // NULL nếu người dùng tự thêm dòng ngoài mẫu (xem ThemDongAsync).
        public int? TieuChiId { get; set; }
        // Nhóm tiêu chí (master) mà dòng này thuộc về — dùng để render tiêu đề
        // nhóm trên Bảng 2-5, giống cách Phieu1ChiTiet.NhomId nhóm checklist
        // Phiếu 1. NULL = dòng không thuộc nhóm nào (bảng chưa có master, hoặc
        // người dùng thêm dòng ngoài mọi nhóm).
        public int? NhomTieuChiId { get; set; }
    }
}
