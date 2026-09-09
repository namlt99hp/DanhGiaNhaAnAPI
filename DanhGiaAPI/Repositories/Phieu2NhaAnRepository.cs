using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu2NhaAnRepository : Repository<Phieu2NhaAn>, IPhieu2NhaAnRepository
    {
        public Phieu2NhaAnRepository(AppDbContext context) : base(context) { }
    }
}
