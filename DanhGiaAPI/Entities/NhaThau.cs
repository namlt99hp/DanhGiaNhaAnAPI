namespace DanhGiaAPI.Entities
{
    public class NhaThau
    {
        public int Id { get; set; }
        public string Ma { get; set; } = null!;
        public string Ten { get; set; } = null!;
        public DateTime? NgayBatDauHd { get; set; }
        public DateTime? NgayKetThucHd { get; set; }
        // Số hợp đồng suất ăn công nghiệp hiện hành của nhà thầu — chỉ dùng để
        // GÁN lúc lập Phiếu 3 mới (xem Phieu3Service.ThemAsync, snapshot vào
        // Phieu3BaoCao.SoHopDong). Sửa giá trị này KHÔNG ảnh hưởng các Phiếu 3
        // đã lập trước đó.
        public string? SoHopDong { get; set; }
        public string TrangThai { get; set; } = "HOAT_DONG";
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
