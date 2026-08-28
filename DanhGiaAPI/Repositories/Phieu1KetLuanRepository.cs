using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu1KetLuanRepository : Repository<Phieu1KetLuan>, IPhieu1KetLuanRepository
    {
        public Phieu1KetLuanRepository(AppDbContext context) : base(context) { }
    }
}
