using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IPhongBanRepository : IRepository<PhongBan>
    {
        Task<PhongBan?> GetByMaAsync(string ma);
    }
}
