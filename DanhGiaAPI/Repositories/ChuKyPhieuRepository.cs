using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class ChuKyPhieuRepository : Repository<ChuKyPhieu>, IChuKyPhieuRepository
    {
        public ChuKyPhieuRepository(AppDbContext context) : base(context) { }
    }
}
