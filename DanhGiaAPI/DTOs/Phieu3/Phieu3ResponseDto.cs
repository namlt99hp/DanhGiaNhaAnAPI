using DanhGiaAPI.Entities;

namespace DanhGiaAPI.DTOs.Phieu3
{
    public class Phieu3Bang2DongDto
    {
        public int Id { get; set; }
        public int PhongBanId { get; set; }
        public List<Phieu3Bang2GiaTri> GiaTri { get; set; } = new();
    }

    public class Phieu3ResponseDto
    {
        public Phieu3BaoCao Phieu { get; set; } = null!;
        public List<Phieu3Bang1Dong> Bang1 { get; set; } = new();
        public List<Phieu3Bang2DongDto> Bang2 { get; set; } = new();
        public Phieu3YKienNhaThau? YKienNhaThau { get; set; }
    }
}
