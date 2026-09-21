using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class PhongBanLoaiPhieuRepository : Repository<PhongBanLoaiPhieu>, IPhongBanLoaiPhieuRepository
    {
        public PhongBanLoaiPhieuRepository(AppDbContext context) : base(context) { }

        public Task<List<PhongBanLoaiPhieu>> GetByPhongBanIdAsync(int phongBanId) =>
            DbSet.Where(x => x.PhongBanId == phongBanId).ToListAsync();
    }
}
