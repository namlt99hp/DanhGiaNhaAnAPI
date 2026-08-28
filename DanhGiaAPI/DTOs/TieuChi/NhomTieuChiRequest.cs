using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.TieuChi
{
    public class NhomTieuChiRequest
    {
        [Required(ErrorMessage = "Loại phiếu không được để trống")]
        [MaxLength(10)]
        public string LoaiPhieu { get; set; } = null!;

        [MaxLength(50)]
        public string? Ma { get; set; }

        [Required(ErrorMessage = "Tên nhóm tiêu chí không được để trống")]
        [MaxLength(255)]
        public string Ten { get; set; } = null!;

        public int ThuTu { get; set; }
        public bool DangHoatDong { get; set; } = true;

        // Chỉ dùng khi LoaiPhieu = "PHIEU4": nhóm thuộc Bảng mấy (2..5).
        public int? SoBang { get; set; }
    }
}
