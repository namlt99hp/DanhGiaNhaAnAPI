using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu3DoanDiaDiemRepository : Repository<Phieu3DoanDiaDiem>, IPhieu3DoanDiaDiemRepository
    {
        public Phieu3DoanDiaDiemRepository(AppDbContext context) : base(context) { }
    }
}
