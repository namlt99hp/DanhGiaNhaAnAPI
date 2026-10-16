using DanhGiaAPI.Common;
using DanhGiaAPI.DTOs.KetQuaDanhGiaLoaiTru;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DanhGiaAPI.Services
{
    // Xem 02. Phantich/modules/KetQuaDanhGiaPhanBo.md, mục "Loại trừ do lỗi
    // thiết bị" — CRUD đánh dấu (Ngày, Bữa ăn, Địa điểm) mà máy kiosk hỏng,
    // không thu được đánh giá thật. Phieu3Service/Phieu4Service
    // (TinhSoLuotCbnvTheoMucTuDoanAsync) đọc trực tiếp
    // IKetQuaDanhGiaLoaiTruRepository để loại trừ khỏi "Lượt đánh giá" khi
    // tổng hợp — không đi qua Service này.
    public class KetQuaDanhGiaLoaiTruService : IKetQuaDanhGiaLoaiTruService
    {
        private readonly IKetQuaDanhGiaLoaiTruRepository _repository;
        private readonly INguoiDungRepository _nguoiDungRepository;
        private readonly IBuaAnRepository _buaAnRepository;
        private readonly IDiaDiemNhaAnRepository _diaDiemNhaAnRepository;
        private readonly IUnitOfWork _unitOfWork;

        public KetQuaDanhGiaLoaiTruService(
            IKetQuaDanhGiaLoaiTruRepository repository,
            INguoiDungRepository nguoiDungRepository,
            IBuaAnRepository buaAnRepository,
            IDiaDiemNhaAnRepository diaDiemNhaAnRepository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _nguoiDungRepository = nguoiDungRepository;
            _buaAnRepository = buaAnRepository;
            _diaDiemNhaAnRepository = diaDiemNhaAnRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<KetQuaDanhGiaLoaiTruDto>> DanhSachAsync(int? diaDiemNhaAnId, int? idBuaAn, DateTime? tuNgay, DateTime? denNgay)
        {
            var query = _repository.Query();
            if (diaDiemNhaAnId.HasValue) query = query.Where(x => x.DiaDiemNhaAnId == diaDiemNhaAnId);
            if (idBuaAn.HasValue) query = query.Where(x => x.ID_BuaAn == idBuaAn);
            if (tuNgay.HasValue) query = query.Where(x => x.Ngay >= tuNgay.Value.Date);
            if (denNgay.HasValue) query = query.Where(x => x.Ngay <= denNgay.Value.Date);

            var ketQua = query.OrderByDescending(x => x.Ngay).ThenByDescending(x => x.ID).ToList();

            var nguoiTaoIds = ketQua.Select(x => x.NguoiTao).Distinct().ToList();
            var tenTheoNguoiTao = nguoiTaoIds.Count > 0
                ? (await _nguoiDungRepository.FindAsync(x => nguoiTaoIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.HoTen)
                : new Dictionary<int, string>();

            // Resolve ID_BuaAn -> tên thật (Sáng/Trưa/Chiều/Đêm) và
            // DiaDiemNhaAnId -> tên địa điểm — trả thẳng text, không để FE
            // (hay người gọi API trực tiếp) phải tự tra thêm.
            var tenTheoBuaAn = (await _buaAnRepository.GetAllAsync()).ToDictionary(x => x.ID, x => x.TenBuaAn);
            var tenTheoDiaDiem = (await _diaDiemNhaAnRepository.GetAllAsync()).ToDictionary(x => x.ID, x => x.DiaDiem);

            return ketQua.Select(x => new KetQuaDanhGiaLoaiTruDto
            {
                ID = x.ID,
                Ngay = x.Ngay,
                ID_BuaAn = x.ID_BuaAn,
                TenBuaAn = tenTheoBuaAn.TryGetValue(x.ID_BuaAn, out var tenBuaAn) ? tenBuaAn : null,
                DiaDiemNhaAnId = x.DiaDiemNhaAnId,
                TenDiaDiem = tenTheoDiaDiem.TryGetValue(x.DiaDiemNhaAnId, out var tenDiaDiem) ? tenDiaDiem : null,
                LyDo = x.LyDo,
                NguoiTao = x.NguoiTao,
                NguoiTaoTen = tenTheoNguoiTao.TryGetValue(x.NguoiTao, out var ten) ? ten : null,
                NgayTao = x.NgayTao,
            }).ToList();
        }

        public async Task<KetQuaDanhGiaLoaiTru> ThemAsync(KetQuaDanhGiaLoaiTruRequest request, int nguoiTaoId)
        {
            var ngay = request.Ngay.Date;
            var daTonTai = await _repository.AnyAsync(x =>
                x.Ngay == ngay && x.ID_BuaAn == request.ID_BuaAn && x.DiaDiemNhaAnId == request.DiaDiemNhaAnId);
            if (daTonTai)
                throw new ApiException("(Ngày, Bữa ăn, Địa điểm) này đã được đánh dấu loại trừ", StatusCodes.Status400BadRequest);

            var entity = new KetQuaDanhGiaLoaiTru
            {
                Ngay = ngay,
                ID_BuaAn = request.ID_BuaAn,
                DiaDiemNhaAnId = request.DiaDiemNhaAnId,
                LyDo = request.LyDo,
                NguoiTao = nguoiTaoId,
                NgayTao = DateTime.Now,
            };
            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return entity;
        }

        public async Task XoaAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new ApiException("Không tìm thấy đánh dấu loại trừ", StatusCodes.Status404NotFound);
            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
