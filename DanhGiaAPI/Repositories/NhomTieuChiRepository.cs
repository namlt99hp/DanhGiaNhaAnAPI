using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class NhomTieuChiRepository : Repository<NhomTieuChi>, INhomTieuChiRepository
    {
        public NhomTieuChiRepository(AppDbContext context) : base(context) { }
    }
}
