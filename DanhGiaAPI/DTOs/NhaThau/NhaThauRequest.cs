using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.NhaThau
{
    public class NhaThauRequest
    {
        [Required(ErrorMessage = "Mã nhà thầu không được để trống")]
        [MaxLength(50)]
        public string Ma { get; set; } = null!;

        [Required(ErrorMessage = "Tên nhà thầu không được để trống")]
        [MaxLength(255)]
        public string Ten { get; set; } = null!;

        public DateTime? NgayBatDauHd { get; set; }
        public DateTime? NgayKetThucHd { get; set; }
        public string TrangThai { get; set; } = "HOAT_DONG";
    }
}
