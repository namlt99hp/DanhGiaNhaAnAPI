using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IQuyenService
    {
        Task<List<Quyen>> DanhSachAsync();
    }
}
