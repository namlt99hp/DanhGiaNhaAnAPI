using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class KetQuaDanhGiaRepository : Repository<KetQuaDanhGia>, IKetQuaDanhGiaRepository
    {
        public KetQuaDanhGiaRepository(AppDbContext context) : base(context) { }
    }
}
