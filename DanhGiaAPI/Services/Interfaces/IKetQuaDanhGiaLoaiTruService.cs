using DanhGiaAPI.DTOs.KetQuaDanhGiaLoaiTru;
using DanhGiaAPI.Models;

namespace DanhGiaAPI.Services.Interfaces
{
    public interface IKetQuaDanhGiaLoaiTruService
    {
        Task<List<KetQuaDanhGiaLoaiTruDto>> DanhSachAsync(int? diaDiemNhaAnId, int? idBuaAn, DateTime? tuNgay, DateTime? denNgay);
        Task<KetQuaDanhGiaLoaiTru> ThemAsync(KetQuaDanhGiaLoaiTruRequest request, int nguoiTaoId);
        Task XoaAsync(int id);
    }
}
