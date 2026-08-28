using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu1ChiTietRepository : Repository<Phieu1ChiTiet>, IPhieu1ChiTietRepository
    {
        public Phieu1ChiTietRepository(AppDbContext context) : base(context) { }
    }
}
