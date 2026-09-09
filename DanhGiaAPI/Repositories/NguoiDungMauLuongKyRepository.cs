using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Repositories
{
    public class NguoiDungMauLuongKyRepository : Repository<NguoiDungMauLuongKy>, INguoiDungMauLuongKyRepository
    {
        public NguoiDungMauLuongKyRepository(AppDbContext context) : base(context) { }

        public Task<List<NguoiDungMauLuongKy>> GetByNguoiDungIdAsync(int nguoiDungId) =>
            DbSet.Where(x => x.NguoiDungId == nguoiDungId).ToListAsync();

        public Task<List<NguoiDungMauLuongKy>> GetByMauLuongKyIdAsync(int mauLuongKyId) =>
            DbSet.Where(x => x.MauLuongKyId == mauLuongKyId).ToListAsync();
    }
}
