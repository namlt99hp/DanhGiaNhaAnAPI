namespace DanhGiaAPI.Services.Interfaces
{
    // Bảo vệ hệ thống khỏi rơi vào trạng thái "không còn ai quản lý được tài
    // khoản" — xem 02. Phantich/modules/VaiTro.md mục rủi ro "tự rút hết quyền
    // quản trị của chính mình". Cả 2 hàm mô phỏng thay đổi TRƯỚC khi ghi DB,
    // ném ApiException nếu sau thay đổi không còn tài khoản HOAT_DONG nào có
    // LaQuanTriVien hoặc quyền QUAN_LY_TAI_KHOAN.
    public interface IQuanTriGuardService
    {
        // Gọi trước khi thay TOÀN BỘ vai trò của 1 tài khoản (NguoiDungService.CapNhatVaiTroAsync).
        Task KiemTraSauKhiDoiVaiTroNguoiDungAsync(int nguoiDungId, List<int> vaiTroIdsMoi);

        // Gọi trước khi sửa định nghĩa 1 VaiTro (LaQuanTriVien/QuyenIds) — VaiTroService.SuaAsync.
        Task KiemTraSauKhiSuaVaiTroAsync(int vaiTroId, bool laQuanTriVienMoi, List<int> quyenIdsMoi);
    }
}
