using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu3YKienNhaThauRepository : Repository<Phieu3YKienNhaThau>, IPhieu3YKienNhaThauRepository
    {
        public Phieu3YKienNhaThauRepository(AppDbContext context) : base(context) { }
    }
}
