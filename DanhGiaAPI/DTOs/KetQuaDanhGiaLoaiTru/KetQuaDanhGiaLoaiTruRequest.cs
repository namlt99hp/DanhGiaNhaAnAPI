namespace DanhGiaAPI.DTOs.KetQuaDanhGiaLoaiTru
{
    public class KetQuaDanhGiaLoaiTruRequest
    {
        public DateTime Ngay { get; set; }
        public int ID_BuaAn { get; set; }
        public int DiaDiemNhaAnId { get; set; }
        public string? LyDo { get; set; }
    }
}
