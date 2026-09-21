using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu4DoanDiaDiemRepository : Repository<Phieu4DoanDiaDiem>, IPhieu4DoanDiaDiemRepository
    {
        public Phieu4DoanDiaDiemRepository(AppDbContext context) : base(context) { }
    }
}
