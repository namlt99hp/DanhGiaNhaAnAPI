using System.Text.Json.Serialization;

namespace DanhGiaAPI.Models
{
    public class KetQuaDanhGiaDTO
    {
        public int ID { get; set; }
        public int DiaDiem_ID { get; set; }
        public string? TenDiaDiem { get; set; }
        public int DiemDanhGia { get; set; }
        public DateTime ThoiGianDanhGia { get; set; }
        public int TieuChi_ID { get; set; }
        public string? TenTieuChi { get; set; }
        public string? CodeBuaAn { get; set; }
        public int? ID_BuaAn { get; set; }
        //public int? ID_LyDo { get; set; }
    }
}
