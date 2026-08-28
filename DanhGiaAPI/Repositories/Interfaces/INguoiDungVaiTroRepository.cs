using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface INguoiDungVaiTroRepository : IRepository<NguoiDungVaiTro>
    {
        Task<List<NguoiDungVaiTro>> GetByNguoiDungIdAsync(int nguoiDungId);

        // Dùng chung cho AuthService (đăng nhập) và NguoiDungService (chi tiết 1 tài khoản)
        Task<List<VaiTro>> GetVaiTroCuaNguoiDungAsync(int nguoiDungId);

        // Dùng cho màn hình danh sách — tránh N+1 khi cần vai trò của nhiều tài khoản 1 lượt.
        Task<List<(int NguoiDungId, string Ma)>> GetVaiTroMapNhieuNguoiDungAsync(List<int> nguoiDungIds);
    }
}
