using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface INguoiDungPhieuQuyenRepository : IRepository<NguoiDungPhieuQuyen>
    {
        Task<List<NguoiDungPhieuQuyen>> GetByNguoiDungIdAsync(int nguoiDungId);
    }
}
