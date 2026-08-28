using DanhGiaAPI.Entities;

namespace DanhGiaAPI.Repositories.Interfaces
{
    // Không dùng IRepository<T> chung: NhatKyChinhSua.Id là BIGINT (long), khác
    // với quy ước Id kiểu int của các entity khác trong IRepository<T>.
    public interface INhatKyChinhSuaRepository
    {
        Task AddAsync(NhatKyChinhSua nhatKy);
        Task<List<NhatKyChinhSua>> LayTheoDoiTuongAsync(string loaiDoiTuong, int doiTuongId);
    }
}
