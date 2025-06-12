using System.Text.Json.Serialization;

namespace DanhGiaAPI.Models
{
    public class KetQuaDanhGia
    {
        public int ID { get; set; }
        public int DiaDiem_ID { get; set; }
        public int DiemDanhGia { get; set; }
        [JsonIgnore]
        public DateTime ThoiGianDanhGia { get; set; }
        [JsonIgnore]
        public int TieuChi_ID { get; set; }
        [JsonIgnore]
        public int? ID_BuaAn { get; set; }
        //public int? ID_LyDo { get; set; }

    }
}
