using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class TepDinhKemRepository : Repository<TepDinhKem>, ITepDinhKemRepository
    {
        public TepDinhKemRepository(AppDbContext context) : base(context) { }
    }
}
