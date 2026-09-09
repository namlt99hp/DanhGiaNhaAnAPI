namespace DanhGiaAPI.Services.Interfaces
{
    public interface ISoHieuService
    {
        // Trả về số thứ tự tiếp theo (đã tăng) cho (loaiPhieu, phamVi, nam, thang) — an toàn
        // khi nhiều request tạo phiếu cùng lúc (dùng MERGE...OUTPUT trong 1 transaction).
        // Phiếu 1: truyền phamVi (mã bếp-phòng ban) + thang (reset theo tháng).
        // Phiếu 2/3/4: phamVi = null, thang = null (1 dãy số duy nhất/năm, không tách theo nhà thầu).
        Task<int> SinhSoTiepTheoAsync(string loaiPhieu, string? phamVi, int nam, int? thang);
    }
}
