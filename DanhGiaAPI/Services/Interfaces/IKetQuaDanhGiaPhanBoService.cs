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
        // 02. Phantich/modules/KetQuaDanhGiaPhanBo.md.
        Task<List<KetQuaDanhGiaPhanBoDanhSachItemDto>> DanhSachAsync(int? diaDiemNhaAnId, int? idBuaAn, DateTime tuNgay, DateTime denNgay);
    }
}
