using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu2KetQuaRepository : Repository<Phieu2KetQua>, IPhieu2KetQuaRepository
    {
        public Phieu2KetQuaRepository(AppDbContext context) : base(context) { }
    }
}
