using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu3DoanRepository : Repository<Phieu3Doan>, IPhieu3DoanRepository
    {
        public Phieu3DoanRepository(AppDbContext context) : base(context) { }
    }
}
