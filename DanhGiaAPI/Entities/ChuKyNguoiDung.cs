namespace DanhGiaAPI.Entities
{
    public class ChuKyNguoiDung
    {
        public int Id { get; set; }
        public int NguoiDungId { get; set; }
        public string DuongDanChuKy { get; set; } = null!;
        public bool DangSuDung { get; set; } = true;
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Soft-delete: ẩn khỏi danh sách chọn/kích hoạt của user, nhưng KHÔNG
        // xóa record lẫn file vật lý — ChuKyPhieuService.TienDoKyAsync tra
        // DuongDanChuKy trực tiếp từ bảng này theo ChuKyId của phiếu ĐÃ ký
        // trong quá khứ (không snapshot đường dẫn); xóa thật sẽ làm mất ảnh
        // chữ ký hiển thị trên các phiếu cũ đã ký bằng chữ ký này.
        public bool DaXoa { get; set; } = false;
    }
}
