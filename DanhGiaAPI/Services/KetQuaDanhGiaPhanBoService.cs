using DanhGiaAPI.DTOs.KetQuaDanhGiaPhanBo;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;

namespace DanhGiaAPI.Services
{
    // Xem 02. Phantich/modules/KetQuaDanhGiaPhanBo.md — phân tích + quyết định
    // nghiệp vụ đã xác nhận.
    //
    // 1 CBNV chỉ được đánh giá tối đa 1 lần/bữa (hoặc không đánh giá) -> tổng
    // lượt đánh giá (KetQuaDanhGia) PHẢI <= số suất ăn thực tế
    // (DuLieuCom.Com_ThucTe_*) tại cùng (Ngày, Bữa ăn, Địa điểm). Khi lệch
    // (trùng đánh giá do lỗi kiosk/mạng), cần phân bổ lại tỷ lệ thuận theo
    // từng mức điểm 1-5, kết quả lưu vào KetQuaDanhGiaPhanBo — Phiếu 3/Phiếu 4
    // đọc từ bảng này thay cho đếm trực tiếp KetQuaDanhGia.
    //
    // Granularity BẮT BUỘC phải là từng (Ngay, ID_BuaAn, DiaDiemNhaAnId) riêng
    // — Com_ThucTe là số theo từng ngày/bữa/địa điểm, không thể gộp nhiều
    // ngày rồi mới co giãn 1 lần (sẽ che lấp ngày không vượt bị co nhầm).
    public class KetQuaDanhGiaPhanBoService : IKetQuaDanhGiaPhanBoService
    {
        private readonly IKetQuaDanhGiaRepository _ketQuaDanhGiaRepository;
        private readonly IDuLieuComRepository _duLieuComRepository;
        private readonly IKetQuaDanhGiaPhanBoRepository _phanBoRepository;
        private readonly IDiaDiemNhaAnRepository _diaDiemNhaAnRepository;
        private readonly IBuaAnRepository _buaAnRepository;
        private readonly IUnitOfWork _unitOfWork;

        public KetQuaDanhGiaPhanBoService(
            IKetQuaDanhGiaRepository ketQuaDanhGiaRepository,
            IDuLieuComRepository duLieuComRepository,
            IKetQuaDanhGiaPhanBoRepository phanBoRepository,
            IDiaDiemNhaAnRepository diaDiemNhaAnRepository,
            IBuaAnRepository buaAnRepository,
            IUnitOfWork unitOfWork)
        {
            _ketQuaDanhGiaRepository = ketQuaDanhGiaRepository;
            _duLieuComRepository = duLieuComRepository;
            _phanBoRepository = phanBoRepository;
            _diaDiemNhaAnRepository = diaDiemNhaAnRepository;
            _buaAnRepository = buaAnRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> TinhLaiTheoNgayBuaAnAsync(DateTime ngay, int idBuaAn)
        {
            var diaDiems = await _diaDiemNhaAnRepository.FindAsync(x => x.IsActive);
            return await TinhLaiAsync(ngay, idBuaAn, diaDiems.Select(x => x.ID).ToList());
        }

        public async Task<int> TinhLaiAsync(DateTime ngay, int idBuaAn, List<int> diaDiemNhaAnIds)
        {
            ngay = ngay.Date;
            if (diaDiemNhaAnIds.Count == 0) return 0;

            // Mapping CodeBuaAn -> field DuLieuCom theo TÊN (khớp
            // Phieu3Service.ComTheoMa) — chỉ xử lý 4 bữa thật Sáng..Đêm
            // ("01".."04"), bỏ qua dòng "ALL".
            var buaAn = await _buaAnRepository.GetByIdAsync(idBuaAn);
            if (buaAn == null || !int.TryParse(buaAn.CodeBuaAn, out var thuTu)) return 0;

            var ketQua = await _ketQuaDanhGiaRepository.FindAsync(x =>
                diaDiemNhaAnIds.Contains(x.DiaDiem_ID) &&
                x.ID_BuaAn == idBuaAn &&
                x.ThoiGianDanhGia.Date == ngay &&
                x.DiemDanhGia >= 1 && x.DiemDanhGia <= 5);

            var duLieuCom = await _duLieuComRepository.FindAsync(x =>
                diaDiemNhaAnIds.Contains(x.ID_DiemAn) && x.Ngay.Date == ngay);
            var comTheoDiaDiem = duLieuCom
                .GroupBy(x => x.ID_DiemAn)
                .ToDictionary(g => g.Key, g => ComTheoThuTu(g.First(), thuTu));

            var hienCo = await _phanBoRepository.FindAsync(x =>
                diaDiemNhaAnIds.Contains(x.DiaDiemNhaAnId) && x.Ngay == ngay && x.ID_BuaAn == idBuaAn);

            var soDongXuLy = 0;
            foreach (var diaDiemId in diaDiemNhaAnIds)
            {
                // Chưa có dữ liệu cơm cho (Ngày, Bữa, Địa điểm) này -> bỏ qua,
                // KHÔNG tạo/ghi đè dòng (tránh ghi 0 sai khi thực ra là "chưa
                // sync", xem comment ComThucTe ở KetQuaDanhGiaPhanBo.cs).
                if (!comTheoDiaDiem.TryGetValue(diaDiemId, out var comThucTe) || comThucTe == null)
                    continue;

                var goc = new int[6]; // [0] không dùng, [1..5]
                foreach (var row in ketQua.Where(r => r.DiaDiem_ID == diaDiemId))
                    goc[row.DiemDanhGia]++;
                var tongGoc = goc[1] + goc[2] + goc[3] + goc[4] + goc[5];

                var dong = hienCo.FirstOrDefault(x => x.DiaDiemNhaAnId == diaDiemId);

                // DuLieuCom là bảng của hệ ngoài (hệ đăng ký cơm), không có
                // cột thời gian cho biết ComThucTe đã CHỐT (sau khi bữa ăn
                // thật sự kết thúc) hay còn TẠM (đang tiếp tục đổi mỗi lần
                // RiceDataController.SyncData chạy) — không sửa schema bảng
                // nguồn được. Thay vào đó: chỉ ghi đè khi input (ComThucTe
                // HOẶC TongGoc) THỰC SỰ khác lần tính trước — giống y thì bỏ
                // qua, giữ nguyên ThoiGianTinh cũ. Nhờ vậy ThoiGianTinh phản
                // ánh đúng lần cuối dữ liệu THỰC SỰ thay đổi — dùng làm dấu
                // hiệu "ComThucTe có thể chưa được đồng bộ số mới" khi rà
                // soát (màn hình KetQuaDanhGiaPhanBoPageV2).
                if (dong != null && dong.ComThucTe == comThucTe && dong.TongGoc == tongGoc)
                    continue;

                // TongGoc <= ComThucTe: trường hợp hợp lệ (không ai đánh giá
                // quá 1 lần) -> giữ nguyên, KHÔNG co giãn (co/phóng cả 2 chiều
                // sẽ bóp méo số liệu thật khi TongGoc < ComThucTe) — xác nhận
                // nghiệp vụ.
                var phanBo = tongGoc <= comThucTe.Value
                    ? new[] { 0, goc[1], goc[2], goc[3], goc[4], goc[5] }
                    : PhanBoTheoSoDuLonNhat(goc, tongGoc, comThucTe.Value);

                var laDongMoi = dong == null;
                dong ??= new KetQuaDanhGiaPhanBo { Ngay = ngay, ID_BuaAn = idBuaAn, DiaDiemNhaAnId = diaDiemId };

                dong.Diem1Goc = goc[1];
                dong.Diem2Goc = goc[2];
                dong.Diem3Goc = goc[3];
                dong.Diem4Goc = goc[4];
                dong.Diem5Goc = goc[5];
                dong.TongGoc = tongGoc;
                dong.ComThucTe = comThucTe;
                dong.Diem1 = phanBo[1];
                dong.Diem2 = phanBo[2];
                dong.Diem3 = phanBo[3];
                dong.Diem4 = phanBo[4];
                dong.Diem5 = phanBo[5];
                dong.Tong = phanBo[1] + phanBo[2] + phanBo[3] + phanBo[4] + phanBo[5];
                dong.ThoiGianTinh = DateTime.Now;

                if (laDongMoi) await _phanBoRepository.AddAsync(dong);
                else _phanBoRepository.Update(dong);
                soDongXuLy++;
            }

            await _unitOfWork.SaveChangesAsync();
            return soDongXuLy;
        }

        public async Task<List<KetQuaDanhGiaPhanBoDanhSachItemDto>> DanhSachAsync(int? diaDiemNhaAnId, int? idBuaAn, DateTime tuNgay, DateTime denNgay)
        {
            var tu = tuNgay.Date;
            var den = denNgay.Date;
            var query = _phanBoRepository.Query().Where(x => x.Ngay >= tu && x.Ngay <= den);
            if (diaDiemNhaAnId.HasValue) query = query.Where(x => x.DiaDiemNhaAnId == diaDiemNhaAnId);
            if (idBuaAn.HasValue) query = query.Where(x => x.ID_BuaAn == idBuaAn);

            var ketQua = query
                .OrderByDescending(x => x.Ngay)
                .ThenBy(x => x.DiaDiemNhaAnId)
                .ThenBy(x => x.ID_BuaAn)
                .ToList();

            var tenBuaAnTheoId = (await _buaAnRepository.GetAllAsync())
                .ToDictionary(b => b.ID, b => b.TenBuaAn);

            return ketQua.Select(x => new KetQuaDanhGiaPhanBoDanhSachItemDto
            {
                ID = x.ID,
                Ngay = x.Ngay,
                ID_BuaAn = x.ID_BuaAn,
                TenBuaAn = tenBuaAnTheoId.GetValueOrDefault(x.ID_BuaAn, $"#{x.ID_BuaAn}"),
                DiaDiemNhaAnId = x.DiaDiemNhaAnId,
                Diem1Goc = x.Diem1Goc,
                Diem2Goc = x.Diem2Goc,
                Diem3Goc = x.Diem3Goc,
                Diem4Goc = x.Diem4Goc,
                Diem5Goc = x.Diem5Goc,
                TongGoc = x.TongGoc,
                ComThucTe = x.ComThucTe,
                Diem1 = x.Diem1,
                Diem2 = x.Diem2,
                Diem3 = x.Diem3,
                Diem4 = x.Diem4,
                Diem5 = x.Diem5,
                Tong = x.Tong,
                ThoiGianTinh = x.ThoiGianTinh,
            }).ToList();
        }

        // Khớp Phieu3Service.ComTheoMa/Phieu4Service.ComTheoMa — mapping theo
        // TÊN field, không theo thứ tự khai báo trong class.
        private static int? ComTheoThuTu(DuLieuCom d, int thuTu) => thuTu switch
        {
            1 => d.Com_ThucTe_Sang,
            2 => d.Com_ThucTe_Trua,
            3 => d.Com_ThucTe_Chieu,
            4 => d.Com_ThucTe_Dem,
            _ => null,
        };

        // Phương pháp SỐ DƯ LỚN NHẤT (Largest Remainder / Hare quota) — co
        // Diem1..5 theo tỷ lệ target/tongGoc, đảm bảo Σ kết quả == target
        // CHÍNH XÁC. Làm tròn từng ô độc lập (VD Math.Round) KHÔNG đảm bảo
        // được điều này (vd tổng gốc 124 ở mức 40/30/20/20/14, target 100 ->
        // làm tròn rời rạc ra 32/24/16/16/11 = 99, thiếu 1).
        private static int[] PhanBoTheoSoDuLonNhat(int[] goc, int tongGoc, int target)
        {
            var ketQua = new int[6];
            var du = new List<(int Muc, decimal Du)>(5);
            var daPhanBo = 0;

            for (var muc = 1; muc <= 5; muc++)
            {
                var tyLe = (decimal)goc[muc] * target / tongGoc;
                var phanNguyen = (int)Math.Floor(tyLe);
                ketQua[muc] = phanNguyen;
                du.Add((muc, tyLe - phanNguyen));
                daPhanBo += phanNguyen;
            }

            var conThieu = target - daPhanBo; // luôn trong [0,4] vì có 5 mức
            foreach (var (muc, _) in du.OrderByDescending(x => x.Du).Take(conThieu))
                ketQua[muc]++;

            return ketQua;
        }
    }
}
