using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IBepAnRepository : IRepository<BepAn>
    {
        Task<BepAn?> GetByMaAsync(string ma);
    }
}
