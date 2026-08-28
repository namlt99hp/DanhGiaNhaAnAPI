using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.TieuChi
{
    public class TieuChiRequest
    {
        public int? NhomId { get; set; }

        [Required(ErrorMessage = "Nội dung tiêu chí không được để trống")]
        public string NoiDung { get; set; } = null!;

        public int ThuTu { get; set; }
        public bool MacDinh { get; set; } = true;
        public bool DangHoatDong { get; set; } = true;

        // Chỉ dùng khi nhóm cha thuộc LoaiPhieu = "PHIEU4" — mẫu seed Phieu4_Dong.
        public string? Dvt { get; set; }
        public string? LoaiDong { get; set; }
        public string? CongThuc { get; set; }
    }
}
