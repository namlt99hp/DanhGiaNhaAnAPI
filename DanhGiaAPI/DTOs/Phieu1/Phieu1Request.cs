using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.Phieu1
{
    public class Phieu1Request
    {
        [Required(ErrorMessage = "Ngày kiểm tra không được để trống")]
        public DateTime NgayKiemTra { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn bếp ăn")]
        public int BepAnId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng ban lập phiếu")]
        public int PhongBanId { get; set; }

        public string? KetLuanGhiChu { get; set; }

        public List<Phieu1ChiTietRequest> ChiTiet { get; set; } = new();
    }
}
