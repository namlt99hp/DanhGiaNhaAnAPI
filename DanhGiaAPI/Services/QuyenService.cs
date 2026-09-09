using DanhGiaAPI.Entities;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services.Interfaces;

namespace DanhGiaAPI.Services
{
    public class QuyenService : IQuyenService
    {
        private readonly IQuyenRepository _quyenRepository;

        public QuyenService(IQuyenRepository quyenRepository)
        {
            _quyenRepository = quyenRepository;
        }

        public async Task<List<Quyen>> DanhSachAsync() => await _quyenRepository.GetAllAsync();
    }
}
