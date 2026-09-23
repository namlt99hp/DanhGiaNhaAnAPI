using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Common
{
    // Danh mục (Nhà thầu / Bếp ăn / Phòng ban / Nhóm tiêu chí / Địa điểm nhà
    // ăn) đã ngừng hoạt động thì KHÔNG được chọn MỚI (tạo phiếu, thêm nhà thầu
    // vào phiếu, đổi sang bản ghi khác...), nhưng bản ghi đã lưu từ trước vẫn
    // giữ nguyên — nơi gọi chỉ kiểm tra khi giá trị thực sự thay đổi, để phiếu
    // cũ vẫn sửa/lưu được (giữ lịch sử).
    // Khớp với FE: src/utils/danhMucHoatDong.ts.
    public static class DanhMucHoatDong
    {
        public const string HoatDong = "HOAT_DONG";

        public static void KiemTraNhaThau(NhaThau nhaThau)
        {
            if (nhaThau.TrangThai != HoatDong)
                throw new ApiException($"Nhà thầu \"{nhaThau.Ten}\" đã ngừng hợp tác, không thể chọn");
        }

        public static void KiemTraBepAn(BepAn bepAn)
        {
            if (bepAn.TrangThai != HoatDong)
                throw new ApiException($"Bếp ăn \"{bepAn.Ten}\" đã ngừng hoạt động, không thể chọn");
        }

        public static void KiemTraPhongBan(PhongBan phongBan)
        {
            if (!phongBan.DangHoatDong)
                throw new ApiException($"Phòng ban \"{phongBan.Ten}\" đã ngừng hoạt động, không thể chọn");
        }

        public static void KiemTraNhomTieuChi(NhomTieuChi nhom)
        {
            if (!nhom.DangHoatDong)
                throw new ApiException($"Nhóm tiêu chí \"{nhom.Ten}\" đã ngừng hoạt động, không thể chọn");
        }

        // Địa điểm nhà ăn chọn MỚI (VD đoạn thời gian Phiếu 3/4 — đoạn luôn
        // tạo mới, không sửa) phải tồn tại và còn hoạt động.
        public static async Task KiemTraDiaDiemNhaAnAsync(IDiaDiemNhaAnRepository repository, IEnumerable<int> ids)
        {
            var danhSachId = ids.Distinct().ToList();
            if (danhSachId.Count == 0) return;

            var danhSach = await repository.FindAsync(x => danhSachId.Contains(x.ID));
            if (danhSach.Count != danhSachId.Count)
                throw new ApiException("Không tìm thấy 1 hoặc nhiều địa điểm nhà ăn đã chọn");

            var ngung = danhSach.FirstOrDefault(x => !x.IsActive);
            if (ngung != null)
                throw new ApiException($"Nhà ăn \"{ngung.DiaDiem}\" đã ngừng hoạt động, không thể chọn");
        }
    }
}
