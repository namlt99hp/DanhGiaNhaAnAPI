using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    public class PhongBanRequest
    {
        [Required(ErrorMessage = "Mã phòng ban không được để trống")]
        [MaxLength(20)]
        public string Ma { get; set; } = null!;

        [Required(ErrorMessage = "Tên phòng ban không được để trống")]
        [MaxLength(255)]
        public string Ten { get; set; } = null!;

        public bool DangHoatDong { get; set; } = true;
    }
}
