using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class BepAnRepository : Repository<BepAn>, IBepAnRepository
    {
        public BepAnRepository(AppDbContext context) : base(context) { }

        public Task<BepAn?> GetByMaAsync(string ma) => DbSet.FirstOrDefaultAsync(x => x.Ma == ma);
    }
}
