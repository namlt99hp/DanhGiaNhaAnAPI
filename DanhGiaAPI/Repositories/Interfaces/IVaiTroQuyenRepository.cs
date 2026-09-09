using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IVaiTroQuyenRepository : IRepository<VaiTroQuyen>
    {
        Task<List<VaiTroQuyen>> GetByVaiTroIdAsync(int vaiTroId);
    }
}
