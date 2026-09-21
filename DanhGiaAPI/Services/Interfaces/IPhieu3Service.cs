using DanhGiaAPI.DTOs.Common;
using DanhGiaAPI.DTOs.Phieu3;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IPhieu3Service
    {
        // nhaThauCuaNguoiGoi: NULL với tài khoản nội bộ; có giá trị với tài
        // khoản nhà thầu -> ép lọc/kiểm tra về đúng nhà thầu đó (xem DangNhap.md).
        // tuNgay/denNgay lọc theo NgayTao (Phiếu 3 là báo cáo theo tháng, không
        // có ngày kiểm tra cụ thể). tuKhoa: lọc SoHieu. chiCuaToi: chỉ phiếu do
        // nguoiDungId tạo.
        Task<PagedResultDto<Phieu3BaoCao>> DanhSachAsync(
            int? nhaThauId, int? thang, int? nam, string? trangThai, DateTime? tuNgay, DateTime? denNgay,
            string? tuKhoa, bool chiCuaToi, int page, int pageSize,
            int? nhaThauCuaNguoiGoi, int? nguoiDungId, bool laAdmin);

        Task<Phieu3ResponseDto> ChiTietAsync(int id, int? nhaThauCuaNguoiGoi, int? nguoiDungId, bool laAdmin);

        // Tạo phiếu + tính tự động Bảng 1 (trừ dòng TONG_SUAT_AN — nhập tay) +
        // khởi tạo khung Bảng 2 (2 dòng PDN/ATMT x 6 tiêu chí, nhập tay hoàn toàn).
        Task<Phieu3ResponseDto> ThemAsync(Phieu3Request request, int? nguoiTaoId, bool laAdmin);

        // Lưu sửa tay Bảng 1 / Bảng 2 — field nào đổi giá trị so với hiện tại thì
        // đánh dấu ChinhSuaThuCong = true và ghi NhatKyChinhSua.
        Task<Phieu3ResponseDto> SuaAsync(int id, Phieu3SuaRequest request, int? nguoiSuaId);

        // Tính lại Bảng 1 từ Phieu2_DanhGia — CHỈ ghi đè các dòng CHƯA bị sửa tay
        // (ChinhSuaThuCong = false), giữ nguyên các ô người dùng đã tự nhập/sửa.
        Task<Phieu3ResponseDto> TinhLaiAsync(int id);

        // laAdmin bypass hoàn toàn ràng buộc "chỉ xóa được khi NHAP" — Admin có
        // thể xóa báo cáo ở BẤT KỲ trạng thái nào từ trang danh sách, kèm dọn
        // dẹp dữ liệu luồng ký (ChuKyPhieu) liên quan — xem Phieu3Service.XoaAsync.
        Task XoaAsync(int id, bool laAdmin);

        Task<Phieu3BaoCao> GuiKyAsync(int id);

        Task<Phieu3BaoCao> DongBoTrangThaiAsync(int id);

        Task<Phieu3ResponseDto> PhanHoiYKienNhaThauAsync(int id, Phieu3YKienNhaThauRequest request);

        // Thêm/xóa 1 "đoạn" thời gian + địa điểm (chỉ khi NHAP/TU_CHOI) —
        // tính lại Bảng 1 ngay sau khi thao tác.
        Task<Phieu3ResponseDto> ThemDoanAsync(int id, DoanRequest request);

        Task<Phieu3ResponseDto> XoaDoanAsync(int id, int doanId);
    }
}
