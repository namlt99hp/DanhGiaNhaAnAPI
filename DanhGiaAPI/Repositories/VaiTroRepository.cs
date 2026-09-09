using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class VaiTroRepository : Repository<VaiTro>, IVaiTroRepository
    {
        public VaiTroRepository(AppDbContext context) : base(context) { }

        public Task<VaiTro?> GetByMaAsync(string ma) => DbSet.FirstOrDefaultAsync(x => x.Ma == ma);

        public Task<List<int>> GetExistingIdsAsync(List<int> ids) =>
            DbSet.Where(x => ids.Contains(x.Id)).Select(x => x.Id).ToListAsync();

        public Task<List<string>> GetMaQuyenChoNhieuVaiTroAsync(List<int> vaiTroIds) =>
            (from vtq in Context.VaiTroQuyen
             join q in Context.Quyen on vtq.QuyenId equals q.Id
             where vaiTroIds.Contains(vtq.VaiTroId)
             select q.Ma).Distinct().ToListAsync();
    }
}
