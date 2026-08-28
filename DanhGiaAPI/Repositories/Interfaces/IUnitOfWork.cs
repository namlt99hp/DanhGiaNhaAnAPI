using Microsoft.EntityFrameworkCore.Storage;

namespace DanhGiaAPI.Repositories.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();

        // Dùng khi 1 nghiệp vụ ghi nhiều bảng qua nhiều lần SaveChangesAsync (VD
        // Phiếu 1: lưu phiếu chính trước để có Id thật, rồi mới lưu chi tiết/kết
        // luận tham chiếu Id đó) nhưng vẫn cần atomic — bọc bằng transaction
        // tường minh thay vì để mỗi SaveChangesAsync tự commit riêng.
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
