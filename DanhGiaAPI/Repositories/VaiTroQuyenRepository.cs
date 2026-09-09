using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class VaiTroQuyenRepository : Repository<VaiTroQuyen>, IVaiTroQuyenRepository
    {
        public VaiTroQuyenRepository(AppDbContext context) : base(context) { }

        public Task<List<VaiTroQuyen>> GetByVaiTroIdAsync(int vaiTroId) =>
            DbSet.Where(x => x.VaiTroId == vaiTroId).ToListAsync();
    }
}
