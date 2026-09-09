using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IVaiTroRepository : IRepository<VaiTro>
    {
        Task<VaiTro?> GetByMaAsync(string ma);
        Task<List<int>> GetExistingIdsAsync(List<int> ids);

        // Mã Quyen (distinct) mà 1 tập VaiTroId gộp lại có được — dùng lúc đăng
        // nhập sinh claim JWT "quyen" (xem AuthService.TaoToken).
        Task<List<string>> GetMaQuyenChoNhieuVaiTroAsync(List<int> vaiTroIds);
    }
}
