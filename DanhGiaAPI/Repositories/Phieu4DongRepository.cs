using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu4DongRepository : Repository<Phieu4Dong>, IPhieu4DongRepository
    {
        public Phieu4DongRepository(AppDbContext context) : base(context) { }
    }
}
