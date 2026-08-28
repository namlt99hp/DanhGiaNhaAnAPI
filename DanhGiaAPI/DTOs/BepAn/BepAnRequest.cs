using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.BepAn
{
    public class BepAnRequest
    {
        [Required(ErrorMessage = "Mã bếp ăn không được để trống")]
        [MaxLength(50)]
        public string Ma { get; set; } = null!;

        [Required(ErrorMessage = "Tên bếp ăn không được để trống")]
        [MaxLength(255)]
        public string Ten { get; set; } = null!;

        public string? ViTri { get; set; }
        public int? NhaThauId { get; set; }
        public string TrangThai { get; set; } = "HOAT_DONG";
    }
}
