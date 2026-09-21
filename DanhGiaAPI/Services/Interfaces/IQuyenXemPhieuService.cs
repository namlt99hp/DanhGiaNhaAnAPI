namespace DanhGiaAPI.Services.Interfaces
{
    // Quyền XEM (danh sách/chi tiết) của 1 tài khoản NỘI BỘ với 1 loại Phiếu —
    // dùng ở Phieu1..4Service.DanhSachAsync/ChiTietAsync. KHÔNG áp dụng cho
    // tài khoản nhà thầu (luật xem của nhà thầu tách riêng, xem
    // GetNhaThauId()/nhaThauCuaNguoiGoi ở từng Service) và KHÔNG bypass cho
    // laAdmin (caller tự kiểm tra laAdmin trước khi gọi, giống hệt
    // KiemTraQuyenDanhGiaAsync hiện có).
    //
    // Công thức: DuocDanhGia=true HOẶC có ít nhất 1 bước ký (NguoiDungMauLuongKy)
    // thuộc đúng LoaiPhieu này — cùng công thức dùng để tính
    // NguoiDungListItemDto.DanhSachLoaiPhieuDuocXem (1 nguồn sự thật duy nhất,
    // xem NguoiDungService.LayDanhSachLoaiPhieuDuocXemAsync).
    public interface IQuyenXemPhieuService
    {
        Task<bool> CoQuyenXemAsync(int nguoiDungId, string loaiPhieu);
        Task<List<string>> DanhSachLoaiPhieuDuocXemAsync(int nguoiDungId);
    }
}
