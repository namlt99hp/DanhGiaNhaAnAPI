using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.LuongKy
{
    public class TuChoiPhieuRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do từ chối")]
        public string GhiChu { get; set; } = null!;
    }
}
