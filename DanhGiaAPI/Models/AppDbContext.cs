//using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DanhGiaAPI.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DiaDiemNhaAn> DiaDiemNhaAn { get; set; }
        public DbSet<KetQuaDanhGia> KetQuaDanhGia { get; set; }
        public DbSet<TieuChiDanhGia> TieuChiDanhGia { get; set; }

        public DbSet<DuLieuCom> DuLieuCom { get; set; }
        public DbSet<BuaAn> BuaAn { get; set; }
        public DbSet<KhungGioDanhGia> KhungGioDanhGia { get; set; }
        //public DbSet<LyDo> LyDo { get; set; }
    }
}
