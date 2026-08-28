using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu2DanhGiaRepository : Repository<Phieu2DanhGia>, IPhieu2DanhGiaRepository
    {
        public Phieu2DanhGiaRepository(AppDbContext context) : base(context) { }
    }
}
