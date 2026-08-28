using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class PhongBanRepository : Repository<PhongBan>, IPhongBanRepository
    {
        public PhongBanRepository(AppDbContext context) : base(context) { }

        public Task<PhongBan?> GetByMaAsync(string ma) => DbSet.FirstOrDefaultAsync(x => x.Ma == ma);
    }
}
