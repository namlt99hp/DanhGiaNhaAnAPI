using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu1KiemTraRepository : Repository<Phieu1KiemTra>, IPhieu1KiemTraRepository
    {
        public Phieu1KiemTraRepository(AppDbContext context) : base(context) { }
    }
}
