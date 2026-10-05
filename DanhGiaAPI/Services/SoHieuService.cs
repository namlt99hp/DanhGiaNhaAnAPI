using System.Data;
using System.Data.Common;
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
        private static readonly HashSet<string> BangPhieuHopLe = new() { "Phieu1_KiemTra", "Phieu2_DanhGia" };

        private readonly AppDbContext _context;

        public SoHieuService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> SinhSoTiepTheoAsync(string loaiPhieu, string? phamVi, int nam, int? thang)
        {
            var connection = await MoKetNoiAsync();

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var ketQua = await TangBoDemAsync(connection, transaction.GetDbTransaction(), loaiPhieu, phamVi, nam, thang);
                await transaction.CommitAsync();
                return ketQua;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<string> CapSoHieuNeuChuaCoAsync(string bangPhieu, int phieuId, string loaiPhieu, string? phamVi, int nam, Func<int, string> dinhDang)
        {
            if (!BangPhieuHopLe.Contains(bangPhieu))
                throw new ArgumentException($"Bảng phiếu không hợp lệ: {bangPhieu}", nameof(bangPhieu));

            var connection = await MoKetNoiAsync();

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var dbTransaction = transaction.GetDbTransaction();

                // UPDLOCK + HOLDLOCK: request thứ 2 cho cùng phiếu phải chờ ở
                // đây tới khi request đầu commit, rồi đọc được số đã cấp — không
                // tăng bộ đếm lần nữa.
                await using (var docPhieu = connection.CreateCommand())
                {
                    docPhieu.Transaction = dbTransaction;
                    docPhieu.CommandText = $"SELECT SoHieu FROM dbo.{bangPhieu} WITH (UPDLOCK, HOLDLOCK) WHERE Id = @Id";
                    AddParam(docPhieu, "@Id", phieuId);

                    var soHieuHienTai = await docPhieu.ExecuteScalarAsync();
                    if (soHieuHienTai == null)
                        throw new InvalidOperationException($"Không tìm thấy phiếu {bangPhieu}#{phieuId}");
                    if (soHieuHienTai is string daCo && !string.IsNullOrEmpty(daCo))
                    {
                        await transaction.CommitAsync();
                        return daCo;
                    }
                }

                var soHieu = dinhDang(await TangBoDemAsync(connection, dbTransaction, loaiPhieu, phamVi, nam, null));

                await using (var ghiPhieu = connection.CreateCommand())
                {
                    ghiPhieu.Transaction = dbTransaction;
                    ghiPhieu.CommandText = $"UPDATE dbo.{bangPhieu} SET SoHieu = @SoHieu WHERE Id = @Id";
                    AddParam(ghiPhieu, "@SoHieu", soHieu);
                    AddParam(ghiPhieu, "@Id", phieuId);
                    await ghiPhieu.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return soHieu;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<DbConnection> MoKetNoiAsync()
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            return connection;
        }

        private static async Task<int> TangBoDemAsync(DbConnection connection, DbTransaction transaction, string loaiPhieu, string? phamVi, int nam, int? thang)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
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

            return (int)(await command.ExecuteScalarAsync())!;
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
