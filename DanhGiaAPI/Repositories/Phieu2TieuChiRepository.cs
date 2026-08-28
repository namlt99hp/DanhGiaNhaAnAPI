using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu2TieuChiRepository : Repository<Phieu2TieuChi>, IPhieu2TieuChiRepository
    {
        public Phieu2TieuChiRepository(AppDbContext context) : base(context) { }
    }
}
