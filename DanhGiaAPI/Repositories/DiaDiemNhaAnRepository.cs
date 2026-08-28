using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class DiaDiemNhaAnRepository : Repository<DiaDiemNhaAn>, IDiaDiemNhaAnRepository
    {
        public DiaDiemNhaAnRepository(AppDbContext context) : base(context) { }
    }
}
