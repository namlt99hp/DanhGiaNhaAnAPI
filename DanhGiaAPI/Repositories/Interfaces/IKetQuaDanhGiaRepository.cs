using DanhGiaAPI.Models;

namespace DanhGiaAPI.Repositories.Interfaces
{
    // KetQuaDanhGia = lượt CBNV tự chấm mức hài lòng 1-5 tại 1 Nhà ăn
    // (DiaDiemNhaAn) qua màn hình kiosk cũ (module 1, xem Models/KetQuaDanhGia.cs).
    // Từ 2026-08-28, Phieu3Service/Phieu4Service dùng dữ liệu này (suy ra nhà
    // thầu phụ trách qua Phieu2_DanhGia.NhaAnId) làm nguồn "Số lượt đánh giá
    // mức 1-5" thay cho proxy Phieu2_KetQua.SoTieuChiDat cũ — xem
    // 02. Phantich/modules/Phieu3_BaoCaoThang.md.
    public interface IKetQuaDanhGiaRepository : IRepository<KetQuaDanhGia>
    {
    }
}
