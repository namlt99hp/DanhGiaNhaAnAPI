using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu4BangRepository : Repository<Phieu4Bang>, IPhieu4BangRepository
    {
        public Phieu4BangRepository(AppDbContext context) : base(context) { }
    }
}
