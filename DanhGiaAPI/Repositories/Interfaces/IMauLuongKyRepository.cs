using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IMauLuongKyRepository : IRepository<MauLuongKy>
    {
        Task<List<int>> GetExistingIdsAsync(List<int> ids);
    }
}
