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

        // Bảng 4/5: dòng này ứng với địa điểm ăn nào — liên kết LOGIC tới
        // dbo.DiaDiemNhaAn.ID (bảng legacy nằm ngoài schema module này, không
        // có FK constraint thật, giống Phieu2DanhGia.NhaAnId).
        public int? DiaDiemNhaAnId { get; set; }

        // CHỈ dùng cho Bảng 5: dòng này thuộc nhà thầu nào (nhóm theo nhà
        // thầu). KHÁC NGHĨA với Phieu4GiaTri.NhaThauId (cột nhà thầu của
        // Bảng 1-3) — đừng nhầm lẫn 2 field cùng tên khác ý nghĩa này.
        public int? NhaThauId { get; set; }

        // Bảng 4/5: giá trị KHÔNG chia theo cột nhà thầu — thay cho cơ chế
        // Phieu4GiaTri (dòng × nhà thầu) dùng ở Bảng 1-3. Từ chỗ nhập tay
        // hoàn toàn, nay TỰ ĐỘNG = tổng DuLieuCom.Com_ThucTe_ALL tại đúng
        // DiaDiemNhaAnId trong [TuNgay, DenNgay] của phiếu (xem
        // TinhLaiGiaTriChungTheoDiaDiemAsync ở Phieu4Service).
        public decimal? GiaTriChung { get; set; }

        // Bảng 4/5: true nếu người dùng đã tự sửa GiaTriChung — chặn không
        // cho lần "tính lại" sau ghi đè (giống Phieu4GiaTri.ChinhSuaThuCong ở
        // Bảng 1-3, ALTER thêm cột khi chuyển GiaTriChung sang tự động).
        public bool ChinhSuaThuCong { get; set; }
    }
}
