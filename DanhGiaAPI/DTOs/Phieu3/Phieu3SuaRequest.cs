namespace DanhGiaAPI.DTOs.Phieu3
{
    // PUT /api/phieu3/{id} — chỉ cho sửa tay Bảng 1 / Bảng 2 (Thang/Nam/NhaThauId
    // cố định từ lúc tạo, quyết định SoHieu nên không cho đổi).
    public class Phieu3SuaRequest
    {
        public List<Phieu3Bang1DongRequest> Bang1 { get; set; } = new();
        public List<Phieu3Bang2DongRequest> Bang2 { get; set; } = new();
    }
}
