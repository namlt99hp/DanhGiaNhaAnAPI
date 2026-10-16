using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class KetQuaDanhGiaLoaiTruRepository : Repository<KetQuaDanhGiaLoaiTru>, IKetQuaDanhGiaLoaiTruRepository
    {
        public KetQuaDanhGiaLoaiTruRepository(AppDbContext context) : base(context) { }
    }
}
