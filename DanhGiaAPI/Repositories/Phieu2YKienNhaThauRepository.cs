using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu2YKienNhaThauRepository : Repository<Phieu2YKienNhaThau>, IPhieu2YKienNhaThauRepository
    {
        public Phieu2YKienNhaThauRepository(AppDbContext context) : base(context) { }
    }
}
