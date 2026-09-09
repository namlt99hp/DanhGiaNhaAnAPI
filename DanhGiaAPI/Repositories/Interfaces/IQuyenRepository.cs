using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IQuyenRepository : IRepository<Quyen>
    {
        Task<List<int>> GetExistingIdsAsync(List<int> ids);
    }
}
