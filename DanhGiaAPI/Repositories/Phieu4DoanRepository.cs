using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu4DoanRepository : Repository<Phieu4Doan>, IPhieu4DoanRepository
    {
        public Phieu4DoanRepository(AppDbContext context) : base(context) { }
    }
}
