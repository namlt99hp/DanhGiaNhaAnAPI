using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class BuaAnRepository : Repository<BuaAn>, IBuaAnRepository
    {
        public BuaAnRepository(AppDbContext context) : base(context) { }
    }
}
