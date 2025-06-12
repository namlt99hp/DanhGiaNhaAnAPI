namespace DanhGiaAPI.Models
{
    public class DuLieuCom
    {
        public int ID { get; set; }
        public DateTime Ngay { get; set; }
        public int ID_DiemAn { get; set; }
        public string CodeDiemAn { get; set; }
        public int? Com_DangKy_ALL { get; set; }
        public int? Com_ThucTe_ALL { get; set; }
        public int? Com_DangKy_Sang { get; set; }
        public int? Com_ThucTe_Sang { get; set; }
        public int? Com_DangKy_Trua { get; set; }
        public int? Com_ThucTe_Trua { get; set; }
        public int? Com_DangKy_Dem { get; set; }
        public int? Com_ThucTe_Dem { get; set; }
        public int? Com_DangKy_Chieu { get; set; }
        public int? Com_ThucTe_Chieu { get; set; }

    }
}
