using System.ComponentModel.DataAnnotations;

namespace DanhGiaAPI.DTOs.Common
{
    // Dùng chung cho POST .../thu-hoi của Phiếu 1 & Phiếu 2 — chỉ là hình dạng
    // request (bắt buộc nêu lý do, giống TuChoiPhieuRequest), KHÔNG kéo theo
    // service/bảng dùng chung (mỗi phiếu có Phieu{N}Service.ThuHoiAsync và
    // Phieu{N}_ThuHoi riêng — xác nhận nghiệp vụ, xem
    // 02. Phantich/modules/Phieu1_KiemTraVSATTP.md).
    public class ThuHoiPhieuRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do thu hồi")]
        public string LyDo { get; set; } = null!;
    }
}
