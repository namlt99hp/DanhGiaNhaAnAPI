using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IPhongBanLoaiPhieuRepository : IRepository<PhongBanLoaiPhieu>
    {
        Task<List<PhongBanLoaiPhieu>> GetByPhongBanIdAsync(int phongBanId);
    }
}
