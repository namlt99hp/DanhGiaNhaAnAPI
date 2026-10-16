using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu1ThuHoiRepository : Repository<Phieu1ThuHoi>, IPhieu1ThuHoiRepository
    {
        public Phieu1ThuHoiRepository(AppDbContext context) : base(context) { }
    }
}
