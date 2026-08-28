using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface INhaThauRepository : IRepository<NhaThau>
    {
        Task<NhaThau?> GetByMaAsync(string ma);
    }
}
