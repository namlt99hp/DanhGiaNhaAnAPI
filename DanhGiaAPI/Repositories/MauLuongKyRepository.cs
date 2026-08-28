using DanhGiaAPI.Entities;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;

namespace DanhGiaAPI.Repositories
{
    public class MauLuongKyRepository : Repository<MauLuongKy>, IMauLuongKyRepository
    {
        public MauLuongKyRepository(AppDbContext context) : base(context) { }
    }
}
