using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class DuLieuComRepository : Repository<DuLieuCom>, IDuLieuComRepository
    {
        public DuLieuComRepository(AppDbContext context) : base(context) { }
    }
}
