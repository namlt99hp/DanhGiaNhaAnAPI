namespace DanhGiaAPI.Services.Interfaces
{
    public interface ISoHieuService
    {
        // Trả về số thứ tự tiếp theo (đã tăng) cho (loaiPhieu, phamVi, nam, thang) — an toàn
        // khi nhiều request tạo phiếu cùng lúc (dùng MERGE...OUTPUT trong 1 transaction).
        // Phiếu 1/2: truyền thang (reset theo tháng). Phiếu 3/4: thang = null (reset theo năm).
        Task<int> SinhSoTiepTheoAsync(string loaiPhieu, string? phamVi, int nam, int? thang);
    }
}
