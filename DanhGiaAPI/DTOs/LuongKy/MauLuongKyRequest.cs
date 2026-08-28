using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.LuongKy
{
    public class MauLuongKyRequest
    {
        [Required(ErrorMessage = "Loại phiếu không được để trống")]
        [MaxLength(10)]
        public string LoaiPhieu { get; set; } = null!;

        [Required(ErrorMessage = "Bước thứ tự không được để trống")]
        public int BuocThuTu { get; set; }

        [Required(ErrorMessage = "Tên bước không được để trống")]
        [MaxLength(255)]
        public string TenBuoc { get; set; } = null!;

        [Required(ErrorMessage = "Loại người ký không được để trống")]
        [MaxLength(20)]
        public string LoaiNguoiKy { get; set; } = null!; // PHONG_BAN / NHA_THAU / VAI_TRO

        public int? PhongBanId { get; set; }
        public int? VaiTroId { get; set; }
        public bool BatBuoc { get; set; } = true;
    }
}
