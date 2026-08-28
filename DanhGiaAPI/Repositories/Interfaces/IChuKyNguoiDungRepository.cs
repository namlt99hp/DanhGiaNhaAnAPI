using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IChuKyNguoiDungRepository : IRepository<ChuKyNguoiDung>
    {
        Task<List<ChuKyNguoiDung>> GetByNguoiDungIdAsync(int nguoiDungId);
        Task<ChuKyNguoiDung?> GetByIdAndNguoiDungIdAsync(int id, int nguoiDungId);
    }
}
