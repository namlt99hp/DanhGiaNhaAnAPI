using DanhGiaAPI.Entities;

namespace DanhGiaAPI.DTOs.Phieu2
{
    public class Phieu2ResponseDto
    {
        public Phieu2DanhGia Phieu { get; set; } = null!;
        public List<Phieu2TieuChi> TieuChi { get; set; } = new();
        public Phieu2KetQua? KetQua { get; set; }
        public Phieu2YKienNhaThau? YKienNhaThau { get; set; }
        public Phieu1KiemTra? Phieu1 { get; set; }
        public Phieu1KetLuan? Phieu1KetLuan { get; set; }
        public global::DanhGiaAPI.Models.DiaDiemNhaAn? NhaAn { get; set; }
    }
}
