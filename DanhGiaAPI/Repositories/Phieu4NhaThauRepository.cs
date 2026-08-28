using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu4NhaThauRepository : Repository<Phieu4NhaThau>, IPhieu4NhaThauRepository
    {
        public Phieu4NhaThauRepository(AppDbContext context) : base(context) { }
    }
}
