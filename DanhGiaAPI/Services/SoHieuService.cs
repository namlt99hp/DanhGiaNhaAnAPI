using System.Data;
using DanhGiaAPI.Models;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DanhGiaAPI.Services
{
    // Không đi qua Repository<T>/IUnitOfWork chung: cần transaction + câu lệnh
    // MERGE...OUTPUT nguyên tử để tăng SoThuTuCuoi an toàn khi nhiều người tạo
    // phiếu cùng lúc (race condition) — Repository<T> generic không hỗ trợ việc
    // này, nên Service tiêm thẳng AppDbContext như 1 ngoại lệ có chủ đích.
    public class SoHieuService : ISoHieuService
    {
        private readonly AppDbContext _context;

        public SoHieuService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> SinhSoTiepTheoAsync(string loaiPhieu, string? phamVi, int nam, int? thang)
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction.GetDbTransaction();
                command.CommandText = @"
                    MERGE dbo.BoDemSoHieu WITH (HOLDLOCK) AS target
                    USING (SELECT @LoaiPhieu AS LoaiPhieu, @PhamVi AS PhamVi, @Nam AS Nam, @Thang AS Thang) AS src
                    ON target.LoaiPhieu = src.LoaiPhieu
                       AND ((target.PhamVi = src.PhamVi) OR (target.PhamVi IS NULL AND src.PhamVi IS NULL))
                       AND target.Nam = src.Nam
                       AND ((target.Thang = src.Thang) OR (target.Thang IS NULL AND src.Thang IS NULL))
                    WHEN MATCHED THEN
                        UPDATE SET SoThuTuCuoi = SoThuTuCuoi + 1
                    WHEN NOT MATCHED THEN
                        INSERT (LoaiPhieu, PhamVi, Nam, Thang, SoThuTuCuoi) VALUES (src.LoaiPhieu, src.PhamVi, src.Nam, src.Thang, 1)
                    OUTPUT INSERTED.SoThuTuCuoi;";

                AddParam(command, "@LoaiPhieu", loaiPhieu);
                AddParam(command, "@PhamVi", (object?)phamVi ?? DBNull.Value);
                AddParam(command, "@Nam", nam);
                AddParam(command, "@Thang", (object?)thang ?? DBNull.Value);

                var ketQua = (int)(await command.ExecuteScalarAsync())!;
                await transaction.CommitAsync();
                return ketQua;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static void AddParam(IDbCommand command, string name, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            command.Parameters.Add(param);
        }
    }
}
