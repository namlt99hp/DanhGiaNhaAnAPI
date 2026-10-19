using DanhGiaAPI.DTOs.KetQuaDanhGiaPhanBo;
using DanhGiaAPI.Models;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IKetQuaDanhGiaPhanBoService
    {
        // Tính lại (upsert) cho TẤT CẢ DiaDiemNhaAn đang hoạt động (IsActive),
        // tại đúng 1 (Ngay, ID_BuaAn) — dùng cho job Quartz/endpoint gọi theo
        // từng bữa ăn vừa kết thúc. Trả về số dòng ĐÃ GHI (bỏ qua các dòng
        // có ComThucTe/TongGoc giống y lần tính trước — xem TinhLaiAsync).
        Task<int> TinhLaiTheoNgayBuaAnAsync(DateTime ngay, int idBuaAn);

        // Tính lại (upsert) cho 1 tập địa điểm cụ thể, tại đúng 1
        // (Ngay, ID_BuaAn) — đơn vị nhỏ nhất bắt buộc phải tính riêng vì
        // Com_ThucTe là số theo từng ngày/bữa/địa điểm. Chỉ ghi đè khi
        // ComThucTe hoặc TongGoc khác lần tính trước (DuLieuCom là bảng hệ
        // ngoài, không có cột timestamp để biết số đã chốt hay còn tạm).
        Task<int> TinhLaiAsync(DateTime ngay, int idBuaAn, List<int> diaDiemNhaAnIds);

        // Danh sách để rà soát số liệu GỐC (Diem1Goc..5Goc/TongGoc) vs số ĐÃ
        // PHÂN BỔ (Diem1..5/Tong) — dùng cho trang màn hình rà soát, xem
        // 02. Phantich/modules/KetQuaDanhGiaPhanBo.md. Sắp xếp mặc định Ngày
        // desc, Bữa ăn desc. daCoGian: true = chỉ lấy dòng Tong != TongGoc
        // ("Đã co giãn"), false = chỉ lấy Tong == TongGoc ("Giữ nguyên"),
        // null = không lọc theo trạng thái.
        Task<List<KetQuaDanhGiaPhanBoDanhSachItemDto>> DanhSachAsync(
            int? diaDiemNhaAnId, int? idBuaAn, DateTime tuNgay, DateTime denNgay, bool? daCoGian = null);

        // Tính lại (upsert) cho CẢ KHOẢNG ngày [tuNgay, denNgay] — dùng cho nút
        // "Phân bổ lại" ở màn KetQuaDanhGiaPhanBoPageV2 (gọi tay theo đúng bộ
        // lọc đang chọn trên màn hình). idBuaAn null = tất cả 4 bữa thật
        // (Sáng..Đêm), diaDiemNhaAnId null = tất cả địa điểm đang IsActive.
        // Với mỗi (Ngay, IdBuaAn): NGÀY ĐÃ QUA (không phải hôm nay) VÀ đã có
        // đủ dòng KetQuaDanhGiaPhanBo (ComThucTe != null) cho hết các địa
        // điểm đang xét -> BỎ QUA, không đọc/tính lại (tối ưu khi quét nhiều
        // ngày/bữa/địa điểm). Hôm nay LUÔN tính lại (không bỏ qua) vì
        // DuLieuCom còn có thể tiếp tục sync trong ngày — xem
        // "Thời điểm tính" ở 02. Phantich/modules/KetQuaDanhGiaPhanBo.md.
        Task<KetQuaDanhGiaPhanBoTinhKhoangResultDto> TinhLaiTheoKhoangAsync(
            DateTime tuNgay, DateTime denNgay, int? idBuaAn, int? diaDiemNhaAnId);
    }
}
