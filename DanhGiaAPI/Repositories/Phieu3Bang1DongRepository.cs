using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu3Bang1DongRepository : Repository<Phieu3Bang1Dong>, IPhieu3Bang1DongRepository
    {
        public Phieu3Bang1DongRepository(AppDbContext context) : base(context) { }
    }
}
