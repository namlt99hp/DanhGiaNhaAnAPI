using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu3Bang2GiaTriRepository : Repository<Phieu3Bang2GiaTri>, IPhieu3Bang2GiaTriRepository
    {
        public Phieu3Bang2GiaTriRepository(AppDbContext context) : base(context) { }
    }
}
