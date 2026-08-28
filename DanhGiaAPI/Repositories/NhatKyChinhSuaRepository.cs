using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class NhatKyChinhSuaRepository : INhatKyChinhSuaRepository
    {
        private readonly AppDbContext _context;

        public NhatKyChinhSuaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(NhatKyChinhSua nhatKy) => await _context.NhatKyChinhSua.AddAsync(nhatKy);

        public async Task<List<NhatKyChinhSua>> LayTheoDoiTuongAsync(string loaiDoiTuong, int doiTuongId) =>
            await _context.NhatKyChinhSua
                .Where(x => x.LoaiDoiTuong == loaiDoiTuong && x.DoiTuongId == doiTuongId)
                .OrderByDescending(x => x.NgayThayDoi)
                .ToListAsync();
    }
}
