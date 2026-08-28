using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IVaiTroRepository : IRepository<VaiTro>
    {
        Task<VaiTro?> GetByMaAsync(string ma);
        Task<List<int>> GetExistingIdsAsync(List<int> ids);
    }
}
