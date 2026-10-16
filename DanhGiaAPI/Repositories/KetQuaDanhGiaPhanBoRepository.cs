using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class KetQuaDanhGiaPhanBoRepository : Repository<KetQuaDanhGiaPhanBo>, IKetQuaDanhGiaPhanBoRepository
    {
        public KetQuaDanhGiaPhanBoRepository(AppDbContext context) : base(context) { }
    }
}
