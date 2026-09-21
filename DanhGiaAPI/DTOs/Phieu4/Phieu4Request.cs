using DanhGiaAPI.DTOs.Common;

namespace DanhGiaAPI.DTOs.Phieu4
{
    // 1 cột nhà thầu + bộ đoạn thời gian/địa điểm CỦA RIÊNG cột đó (tương
    // đương 1 "Phiếu 3 con") — thay cho "NhaThauIds: List<int>" phẳng cũ.
    public class Phieu4NhaThauRequest
    {
        public int NhaThauId { get; set; }
        public List<DoanRequest> Doan { get; set; } = new();
    }

    public class Phieu4Request
    {
        // Khoảng ngày lập phiếu — người dùng chọn TRỰC TIẾP lúc tạo (popup
        // "Lập phiếu mới"/form tạo mới), CỐ ĐỊNH trong suốt vòng đời phiếu
        // (không tự tính lại theo đoạn nữa như trước — xem
        // Phieu4Service.ThemAsync). Mọi "đoạn" khai báo sau (kể cả lúc tạo
        // lẫn thêm sau ở trang chi tiết) phải nằm TRỌN trong khoảng này, xem
        // Phieu4Service.KiemTraDoanHopLe.
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public List<Phieu4NhaThauRequest> NhaThau { get; set; } = new();
    }
}
