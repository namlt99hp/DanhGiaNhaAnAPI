using DanhGiaAPI.DTOs.Phieu3;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IPhieu3Service
    {
        Task<List<Phieu3BaoCao>> DanhSachAsync(int? nhaThauId, int? thang, int? nam, string? trangThai);

        Task<Phieu3ResponseDto> ChiTietAsync(int id);

        // Tạo phiếu + tính tự động Bảng 1 (trừ dòng TONG_SUAT_AN — nhập tay) +
        // khởi tạo khung Bảng 2 (2 dòng PDN/ATMT x 6 tiêu chí, nhập tay hoàn toàn).
        Task<Phieu3ResponseDto> ThemAsync(Phieu3Request request, int? nguoiTaoId);

        // Lưu sửa tay Bảng 1 / Bảng 2 — field nào đổi giá trị so với hiện tại thì
        // đánh dấu ChinhSuaThuCong = true và ghi NhatKyChinhSua.
        Task<Phieu3ResponseDto> SuaAsync(int id, Phieu3SuaRequest request, int? nguoiSuaId);

        // Tính lại Bảng 1 từ Phieu2_DanhGia — CHỈ ghi đè các dòng CHƯA bị sửa tay
        // (ChinhSuaThuCong = false), giữ nguyên các ô người dùng đã tự nhập/sửa.
        Task<Phieu3ResponseDto> TinhLaiAsync(int id);

        Task XoaAsync(int id);

        Task<Phieu3BaoCao> GuiKyAsync(int id);

        Task<Phieu3BaoCao> DongBoTrangThaiAsync(int id);

        Task<Phieu3ResponseDto> PhanHoiYKienNhaThauAsync(int id, Phieu3YKienNhaThauRequest request);
    }
}
