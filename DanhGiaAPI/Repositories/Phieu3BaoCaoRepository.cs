using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu3BaoCaoRepository : Repository<Phieu3BaoCao>, IPhieu3BaoCaoRepository
    {
        public Phieu3BaoCaoRepository(AppDbContext context) : base(context) { }
    }
}
