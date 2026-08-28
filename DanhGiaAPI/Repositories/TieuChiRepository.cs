using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class TieuChiRepository : Repository<TieuChi>, ITieuChiRepository
    {
        public TieuChiRepository(AppDbContext context) : base(context) { }
    }
}
