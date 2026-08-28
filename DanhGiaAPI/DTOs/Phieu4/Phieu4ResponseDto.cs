using DanhGiaAPI.Entities;

namespace DanhGiaAPI.DTOs.Phieu4
{
    public class Phieu4DongDto
    {
        public int Id { get; set; }
        public int BangId { get; set; }
        public int? NhomSo { get; set; }
        public int? Stt { get; set; }
        public string? NoiDung { get; set; }
        public string? Dvt { get; set; }
        public string? LoaiDong { get; set; }
        public string? CongThuc { get; set; }
        public int? TieuChiId { get; set; }
        public int? NhomTieuChiId { get; set; }
        public List<Phieu4GiaTri> GiaTri { get; set; } = new();
    }

    public class Phieu4BangDto
    {
        public int Id { get; set; }
        public int SoBang { get; set; }
        public string? TenBang { get; set; }
        public List<Phieu4DongDto> Dong { get; set; } = new();
    }

    public class Phieu4ResponseDto
    {
        public Phieu4TongHop Phieu { get; set; } = null!;
        public List<Phieu4NhaThau> NhaThau { get; set; } = new();
        public List<Phieu4BangDto> Bang { get; set; } = new();
    }
}
