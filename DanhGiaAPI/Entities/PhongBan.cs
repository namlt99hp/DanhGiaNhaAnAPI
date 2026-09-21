using System.ComponentModel.DataAnnotations.Schema;

namespace DanhGiaAPI.Entities
{
    public class PhongBan
    {
        public int Id { get; set; }
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public bool DangHoatDong { get; set; }

        // Loại phiếu phòng ban này ĐƯỢC PHÉP xử lý (trần cấu trúc, xem
        // PhongBanLoaiPhieu) — không map cột riêng, PhongBanService tự nạp từ
        // bảng PhongBanLoaiPhieu trước khi trả về. Rỗng = không giới hạn.
        [NotMapped]
        public List<string> DanhSachLoaiPhieu { get; set; } = new();
    }
}
