using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class NguoiDungPhieuQuyenRepository : Repository<NguoiDungPhieuQuyen>, INguoiDungPhieuQuyenRepository
    {
        public NguoiDungPhieuQuyenRepository(AppDbContext context) : base(context) { }

        public Task<List<NguoiDungPhieuQuyen>> GetByNguoiDungIdAsync(int nguoiDungId) =>
            DbSet.Where(x => x.NguoiDungId == nguoiDungId).ToListAsync();
    }
}
