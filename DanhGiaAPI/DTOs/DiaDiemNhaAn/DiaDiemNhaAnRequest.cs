using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.DiaDiemNhaAn
{
    public class DiaDiemNhaAnRequest
    {
        [Required(ErrorMessage = "Tên địa điểm không được để trống")]
        [MaxLength(255)]
        public string DiaDiem { get; set; } = null!;

        [MaxLength(50)]
        public string? CodeDiemAn { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
