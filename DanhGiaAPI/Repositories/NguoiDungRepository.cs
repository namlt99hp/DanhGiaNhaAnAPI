using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class NguoiDungRepository : Repository<NguoiDung>, INguoiDungRepository
    {
        public NguoiDungRepository(AppDbContext context) : base(context) { }

        public Task<NguoiDung?> GetByTenDangNhapAsync(string tenDangNhap) =>
            DbSet.FirstOrDefaultAsync(x => x.TenDangNhap == tenDangNhap);

        public Task<bool> TonTaiEmailAsync(string email) =>
            DbSet.AnyAsync(x => x.Email == email);
    }
}
