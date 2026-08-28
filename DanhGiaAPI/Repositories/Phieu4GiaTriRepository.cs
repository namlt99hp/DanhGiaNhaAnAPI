using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu4GiaTriRepository : Repository<Phieu4GiaTri>, IPhieu4GiaTriRepository
    {
        public Phieu4GiaTriRepository(AppDbContext context) : base(context) { }
    }
}
