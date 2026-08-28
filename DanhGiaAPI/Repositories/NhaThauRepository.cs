using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class NhaThauRepository : Repository<NhaThau>, INhaThauRepository
    {
        public NhaThauRepository(AppDbContext context) : base(context) { }

        public Task<NhaThau?> GetByMaAsync(string ma) => DbSet.FirstOrDefaultAsync(x => x.Ma == ma);
    }
}
