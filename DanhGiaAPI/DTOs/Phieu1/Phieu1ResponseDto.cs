using DanhGiaAPI.Entities;

namespace DanhGiaAPI.DTOs.Phieu1
{
    public class Phieu1ResponseDto
    {
        public Phieu1KiemTra Phieu { get; set; } = null!;
        public List<Phieu1ChiTiet> ChiTiet { get; set; } = new();
        public Phieu1KetLuan? KetLuan { get; set; }
    }
}
