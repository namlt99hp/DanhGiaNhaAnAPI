using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface INguoiDungMauLuongKyRepository : IRepository<NguoiDungMauLuongKy>
    {
        Task<List<NguoiDungMauLuongKy>> GetByNguoiDungIdAsync(int nguoiDungId);
        Task<List<NguoiDungMauLuongKy>> GetByMauLuongKyIdAsync(int mauLuongKyId);
    }
}
