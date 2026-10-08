namespace DanhGiaAPI.Models
{
    // Gửi qua POST + JSON body (không phải query string) — model binding
    // mặc định của ASP.NET Core cho List<T> từ query string chỉ nhận dạng
    // lặp key ("DiaDiemIds=8&DiaDiemIds=6"), không nhận dạng chuỗi nối phẩy
    // mà RTK Query/URLSearchParams tự sinh khi value là array ("DiaDiemIds=8,6")
    // — JSON body thì deserialize List<T> đúng theo đúng những gì FE gửi,
    // không cần hack gì thêm.
    public class DashboardFilterParameters
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public List<int>? DiaDiemIds { get; set; }

        // Mỗi filter dưới đây là 1 nhóm chọn-nhiều độc lập (OR giữa các lựa
        // chọn trong cùng nhóm), các nhóm kết hợp với nhau bằng AND — ví dụ
        // chọn Tuần 44 + Tuần 45 và Bữa ăn Sáng nghĩa là "(tuần 44 HOẶC tuần
        // 45) VÀ bữa Sáng". Không đụng tới TuNgay/DenNgay (vẫn là khoảng bao
        // ngoài độc lập, không tự nhảy theo Tuần/Tháng được chọn).
        public List<string>? CodeBuaAnList { get; set; }

        // Định dạng "{năm}-{số tuần ISO}", ví dụ "2026-44".
        public List<string>? TuanDaChon { get; set; }

        // Định dạng "{năm}-{tháng 2 số}", ví dụ "2026-10".
        public List<string>? ThangDaChon { get; set; }
    }

    public class MucDanhGiaThongKeDTO
    {
        public int DiemDanhGia { get; set; }
        public int SoLuong { get; set; }
        public double TyLePercent { get; set; }
    }

    public class TyLeTheoNgayDTO
    {
        public DateTime Ngay { get; set; }
        public int SoLuotDanhGia { get; set; }
        public long SoComThucTe { get; set; }
        public double TyLePercent { get; set; }
    }

    public class DiaDiemThongKeDTO
    {
        public int DiaDiemId { get; set; }
        public string TenDiaDiem { get; set; }
        public int Muc1 { get; set; }
        public int Muc2 { get; set; }
        public int Muc3 { get; set; }
        public int Muc4 { get; set; }
        public int Muc5 { get; set; }
    }

    public class DashboardSummaryDTO
    {
        public int TongLuotDanhGia { get; set; }
        public long TongComThucTe { get; set; }
        public double TyLeDanhGiaPercent { get; set; }
        public List<MucDanhGiaThongKeDTO> TheoMucDanhGia { get; set; } = new();
        public List<TyLeTheoNgayDTO> TheoNgay { get; set; } = new();
        public List<DiaDiemThongKeDTO> TheoDiaDiem { get; set; } = new();
    }
}
