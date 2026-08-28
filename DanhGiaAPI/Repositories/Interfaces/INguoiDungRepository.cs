using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface INguoiDungRepository : IRepository<NguoiDung>
    {
        Task<NguoiDung?> GetByTenDangNhapAsync(string tenDangNhap);
        Task<bool> TonTaiEmailAsync(string email);
    }
}
