using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IPhienDangNhapRepository : IRepository<PhienDangNhap>
    {
        Task<PhienDangNhap?> GetByTokenAsync(string token);
    }
}
