using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu4TongHopRepository : Repository<Phieu4TongHop>, IPhieu4TongHopRepository
    {
        public Phieu4TongHopRepository(AppDbContext context) : base(context) { }
    }
}
