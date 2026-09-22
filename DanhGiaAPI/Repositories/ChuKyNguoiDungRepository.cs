using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class ChuKyNguoiDungRepository : Repository<ChuKyNguoiDung>, IChuKyNguoiDungRepository
    {
        public ChuKyNguoiDungRepository(AppDbContext context) : base(context) { }

        public Task<List<ChuKyNguoiDung>> GetByNguoiDungIdAsync(int nguoiDungId) =>
            DbSet.Where(x => x.NguoiDungId == nguoiDungId && !x.DaXoa).OrderByDescending(x => x.NgayTao).ToListAsync();

        public Task<ChuKyNguoiDung?> GetByIdAndNguoiDungIdAsync(int id, int nguoiDungId) =>
            DbSet.FirstOrDefaultAsync(x => x.Id == id && x.NguoiDungId == nguoiDungId && !x.DaXoa);
    }
}
