using DanhGiaAPI.DTOs.Common;
using DanhGiaAPI.Entities;

namespace DanhGiaAPI.DTOs.Phieu4
{
    // Thay cho entity thô Phieu4NhaThau — gắn kèm bộ đoạn của riêng cột này.
    public class Phieu4NhaThauDto
    {
        public int Id { get; set; }
        public int PhieuId { get; set; }
        public int NhaThauId { get; set; }
        public int ThuTu { get; set; }
        public List<DoanDto> Doan { get; set; } = new();
    }

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
        public int? DiaDiemNhaAnId { get; set; }
        public int? NhaThauId { get; set; }
        public decimal? GiaTriChung { get; set; }
        public bool ChinhSuaThuCong { get; set; }
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
        public List<Phieu4NhaThauDto> NhaThau { get; set; } = new();
        public List<Phieu4BangDto> Bang { get; set; } = new();
    }
}
