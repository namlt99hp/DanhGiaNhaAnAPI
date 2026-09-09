using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class QuyenRepository : Repository<Quyen>, IQuyenRepository
    {
        public QuyenRepository(AppDbContext context) : base(context) { }

        public Task<List<int>> GetExistingIdsAsync(List<int> ids) =>
            DbSet.Where(x => ids.Contains(x.Id)).Select(x => x.Id).ToListAsync();
    }
}
