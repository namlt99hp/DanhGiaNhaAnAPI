using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class Phieu2ThuHoiRepository : Repository<Phieu2ThuHoi>, IPhieu2ThuHoiRepository
    {
        public Phieu2ThuHoiRepository(AppDbContext context) : base(context) { }
    }
}
