using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class NguoiDungVaiTroRepository : Repository<NguoiDungVaiTro>, INguoiDungVaiTroRepository
    {
        private readonly AppDbContext _context;

        public NguoiDungVaiTroRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<List<NguoiDungVaiTro>> GetByNguoiDungIdAsync(int nguoiDungId) =>
            DbSet.Where(x => x.NguoiDungId == nguoiDungId).ToListAsync();

        public Task<List<VaiTro>> GetVaiTroCuaNguoiDungAsync(int nguoiDungId) =>
            (from vt in _context.VaiTro
             join ndvt in DbSet on vt.Id equals ndvt.VaiTroId
             where ndvt.NguoiDungId == nguoiDungId
             select vt).ToListAsync();

        public async Task<List<(int NguoiDungId, string Ma)>> GetVaiTroMapNhieuNguoiDungAsync(List<int> nguoiDungIds)
        {
            var ketQua = await (from ndvt in DbSet
                                 join vt in _context.VaiTro on ndvt.VaiTroId equals vt.Id
                                 where nguoiDungIds.Contains(ndvt.NguoiDungId)
                                 select new { ndvt.NguoiDungId, vt.Ma }).ToListAsync();

            return ketQua.Select(x => (x.NguoiDungId, x.Ma)).ToList();
        }
    }
}
