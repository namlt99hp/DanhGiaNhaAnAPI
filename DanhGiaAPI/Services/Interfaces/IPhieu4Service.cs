using DanhGiaAPI.DTOs.Phieu4;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IPhieu4Service
    {
        Task<List<Phieu4TongHop>> DanhSachAsync(string? trangThai);

        Task<Phieu4ResponseDto> ChiTietAsync(int id);

        // Tạo phiếu + chọn cột nhà thầu + khởi tạo Bảng 1 (cấu trúc cố định,
        // tự tính ngay) + khởi tạo khung Bảng 2-5 (rỗng, nội dung tự do).
        Task<Phieu4ResponseDto> ThemAsync(Phieu4Request request, int? nguoiTaoId);

        Task XoaAsync(int id);

        // Thêm 1 cột nhà thầu vào phiếu đã lập (chỉ khi NHAP/TU_CHOI) — tạo ô
        // giá trị rỗng cho nhà thầu mới ở mọi dòng của mọi bảng (1-5), rồi tính
        // lại Bảng 1 ngay để cột mới có dữ liệu tự động như các cột khác.
        Task<Phieu4ResponseDto> ThemNhaThauAsync(int id, int nhaThauId);

        // Xóa 1 cột nhà thầu khỏi phiếu đã lập (chỉ khi NHAP/TU_CHOI, phải còn
        // lại ít nhất 1 nhà thầu) — xóa toàn bộ ô giá trị (Bảng 1-3) của nhà
        // thầu này ở mọi dòng, kèm dòng Bảng 5 gắn cứng với nhà thầu này
        // (Phieu4Dong.NhaThauId).
        Task<Phieu4ResponseDto> XoaNhaThauAsync(int id, int nhaThauId);

        // Tính lại Bảng 1 (nhóm 2/3/4) từ Phieu2_DanhGia trong khoảng ngày —
        // CHỈ ghi đè các ô CHƯA bị sửa tay (ChinhSuaThuCong = false).
        Task<Phieu4ResponseDto> TinhLaiAsync(int id);

        // Sửa tay 1 hoặc nhiều ô cùng lúc (mọi bảng, kể cả Bảng 1 nhóm 1) —
        // đánh dấu ChinhSuaThuCong = true + ghi NhatKyChinhSua cho ô nào đổi giá trị.
        Task<Phieu4ResponseDto> CapNhatGiaTriAsync(int id, Phieu4CapNhatGiaTriRequest request, int? nguoiSuaId);

        // Sửa tên Bảng (áp dụng cho mọi bảng, kể cả đổi tên Bảng 1 nếu cần).
        // Bảng 2-5 KHÔNG còn API thêm/sửa/xóa dòng — nội dung dòng cố định
        // theo NhomTieuChi/TieuChi (master), tự đồng bộ mỗi lần ChiTietAsync
        // (xem ChiTietAsync/DongBoDongTuMauAsync trong Phieu4Service).
        Task<Phieu4ResponseDto> SuaBangAsync(int id, int bangId, Phieu4BangRequest request);

        Task<Phieu4TongHop> GuiKyAsync(int id);

        Task<Phieu4TongHop> DongBoTrangThaiAsync(int id);
    }
}
