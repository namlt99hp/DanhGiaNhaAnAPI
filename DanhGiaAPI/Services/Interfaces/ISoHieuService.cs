namespace DanhGiaAPI.Services.Interfaces
{
    public interface ISoHieuService
    {
        // Trả về số thứ tự tiếp theo (đã tăng) cho (loaiPhieu, phamVi, nam, thang) — an toàn
        // khi nhiều request tạo phiếu cùng lúc (dùng MERGE...OUTPUT trong 1 transaction).
        // Phiếu 1: truyền phamVi (mã bếp-phòng ban) + thang (reset theo tháng).
        // Phiếu 2/3/4: phamVi = null, thang = null (1 dãy số duy nhất/năm, không tách theo nhà thầu).
        Task<int> SinhSoTiepTheoAsync(string loaiPhieu, string? phamVi, int nam, int? thang);

        // Cấp số hiệu cho 1 phiếu ĐÃ TỒN TẠI nếu nó chưa có (Phiếu 1/2: chỉ cấp
        // khi hoàn tất ký duyệt, để phiếu Nháp bị xóa không làm khuyết số).
        // Khóa dòng phiếu + tăng bộ đếm + ghi SoHieu trong CÙNG 1 transaction:
        // gọi lặp/đồng thời cho cùng phiếu chỉ tốn đúng 1 số, các lần sau trả
        // về số đã cấp. bangPhieu là tên bảng cố định trong code (không nhận
        // từ input người dùng).
        Task<string> CapSoHieuNeuChuaCoAsync(string bangPhieu, int phieuId, string loaiPhieu, string? phamVi, int nam, Func<int, string> dinhDang);
    }
}
