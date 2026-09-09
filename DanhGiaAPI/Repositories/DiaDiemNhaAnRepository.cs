using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class DiaDiemNhaAnRepository : Repository<DiaDiemNhaAn>, IDiaDiemNhaAnRepository
    {
        public DiaDiemNhaAnRepository(AppDbContext context) : base(context) { }

        public Task<DiaDiemNhaAn?> GetByTenAsync(string diaDiem) =>
            DbSet.FirstOrDefaultAsync(x => x.DiaDiem == diaDiem);
    }
}
