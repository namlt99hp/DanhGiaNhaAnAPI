using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    public class VaiTroRequest
    {
        [Required(ErrorMessage = "Mã vai trò không được để trống")]
        [MaxLength(50)]
        public string Ma { get; set; } = null!;

        [Required(ErrorMessage = "Tên vai trò không được để trống")]
        [MaxLength(255)]
        public string Ten { get; set; } = null!;

        public bool CoQuyenDuyetTk { get; set; }
    }
}
