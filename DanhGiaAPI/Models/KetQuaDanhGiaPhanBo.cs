namespace DanhGiaAPI.Models
{
    // Số lượt đánh giá CBNV (KetQuaDanhGia, hệ kiosk) đã được PHÂN BỔ LẠI cho
    // khớp với số suất ăn thực tế (DuLieuCom.Com_ThucTe_*), tại đúng 1
    // (Ngay, ID_BuaAn, DiaDiemNhaAnId) — đơn vị nhỏ nhất bắt buộc phải tính
    // riêng vì Com_ThucTe là số theo từng ngày/bữa/địa điểm, không thể gộp
    // nhiều ngày rồi mới co giãn 1 lần (xem 02. Phantich/modules/KetQuaDanhGiaPhanBo.md).
    //
    // Quy tắc tính (KetQuaDanhGiaPhanBoService.TinhLaiAsync):
    //   - TongGoc <= ComThucTe (trường hợp hợp lệ, không ai đánh giá quá 1
    //     lần): giữ nguyên, Diem1..5 = Diem1Goc..5Goc.
    //   - TongGoc > ComThucTe (trùng đánh giá): co theo tỷ lệ
    //     ComThucTe/TongGoc, làm tròn bằng phương pháp SỐ DƯ LỚN NHẤT (Largest
    //     Remainder) để đảm bảo Tong == ComThucTe chính xác (không dùng
    //     Math.Round từng ô rời rạc — có thể lệch tổng).
    //   - ComThucTe == NULL (DuLieuCom chưa sync cho ngày/bữa/địa điểm này):
    //     KHÔNG tạo/ghi đè dòng.
    //   - ComThucTe và TongGoc đều GIỐNG lần tính trước (dòng đã tồn tại):
    //     KHÔNG ghi đè, giữ nguyên ThoiGianTinh cũ — DuLieuCom (hệ ngoài,
    //     không có cột timestamp sync) chưa có gì mới kể từ lần tính trước,
    //     ThoiGianTinh dùng làm dấu hiệu nhận biết "có thể chưa sync số mới".
    public class KetQuaDanhGiaPhanBo
    {
        public int ID { get; set; }
        public DateTime Ngay { get; set; }
        public int ID_BuaAn { get; set; }
        public int DiaDiemNhaAnId { get; set; }

        // Số đánh giá THÔ trước khi phân bổ — giữ lại để audit/đối chiếu.
        public int Diem1Goc { get; set; }
        public int Diem2Goc { get; set; }
        public int Diem3Goc { get; set; }
        public int Diem4Goc { get; set; }
        public int Diem5Goc { get; set; }
        public int TongGoc { get; set; }

        // Snapshot DuLieuCom.Com_ThucTe_{Sang,Trua,Chieu,Dem} lúc tính — NULL
        // nghĩa là chưa sync, dòng này sẽ không được tạo (xem lớp Service).
        public int? ComThucTe { get; set; }

        // Số ĐÃ PHÂN BỔ — Phieu3Service/Phieu4Service đọc các cột này thay cho
        // đếm trực tiếp KetQuaDanhGia.
        public int Diem1 { get; set; }
        public int Diem2 { get; set; }
        public int Diem3 { get; set; }
        public int Diem4 { get; set; }
        public int Diem5 { get; set; }
        public int Tong { get; set; }

        public DateTime ThoiGianTinh { get; set; }
    }
}
