using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu3Bang2DongRepository : Repository<Phieu3Bang2Dong>, IPhieu3Bang2DongRepository
    {
        public Phieu3Bang2DongRepository(AppDbContext context) : base(context) { }
    }
}
