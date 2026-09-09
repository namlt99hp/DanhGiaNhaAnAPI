using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.QuanLyTaiKhoan
{
    public class NguoiDungPhieuQuyenItemDto
    {
        [Required(ErrorMessage = "Loại phiếu không được để trống")]
        public string LoaiPhieu { get; set; } = null!;

        public bool DuocDanhGia { get; set; }
        public bool DuocQuanLyTieuChi { get; set; }
    }
}
