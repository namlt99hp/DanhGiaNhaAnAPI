using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class PhienDangNhapRepository : Repository<PhienDangNhap>, IPhienDangNhapRepository
    {
        public PhienDangNhapRepository(AppDbContext context) : base(context) { }

        public Task<PhienDangNhap?> GetByTokenAsync(string token) =>
            DbSet.FirstOrDefaultAsync(x => x.MaToken == token);
    }
}
