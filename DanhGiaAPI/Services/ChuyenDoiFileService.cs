using System.ComponentModel;
using System.Diagnostics;
using DanhGiaAPI.Common;
using DanhGiaAPI.Services.Interfaces;

namespace DanhGiaAPI.Services
{
    // Gọi LibreOffice headless (soffice --convert-to pdf) để convert docx sang
    // pdf — cần cài LibreOffice trên server chạy API này. Cấu hình đường dẫn
    // thực thi khác mặc định qua appsettings "LibreOffice:SofficePath" (VD khi
    // Windows cài lệch thư mục, hoặc Linux không có soffice trong PATH).
    public class ChuyenDoiFileService : IChuyenDoiFileService
    {
        private readonly string _duongDanSoffice;
        private readonly int _thoiGianChoToiDaGiay;

        public ChuyenDoiFileService(IConfiguration configuration)
        {
            var duongDanCauHinh = configuration["LibreOffice:SofficePath"];
            _duongDanSoffice = string.IsNullOrWhiteSpace(duongDanCauHinh) ? DuongDanMacDinh() : duongDanCauHinh;
            _thoiGianChoToiDaGiay = configuration.GetValue<int?>("LibreOffice:TimeoutSeconds") ?? 60;
        }

        private static string DuongDanMacDinh() =>
            OperatingSystem.IsWindows()
                ? @"C:\Program Files\LibreOffice\program\soffice.exe"
                : "soffice";

        public async Task<byte[]> DocxSangPdfAsync(byte[] docxBytes)
        {
            // Thư mục làm việc + UserInstallation RIÊNG cho MỖI lần convert —
            // nhiều request đồng thời dùng chung 1 profile LibreOffice sẽ báo
            // lỗi "another instance is running" và convert âm thầm thất bại.
            var thuMucLamViec = Path.Combine(Path.GetTempPath(), "docx-to-pdf-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(thuMucLamViec);
            var duongDanDocx = Path.Combine(thuMucLamViec, "input.docx");
            var duongDanPdf = Path.Combine(thuMucLamViec, "input.pdf");
            var duongDanProfile = Path.Combine(thuMucLamViec, "profile").Replace('\\', '/');

            try
            {
                await File.WriteAllBytesAsync(duongDanDocx, docxBytes);

                var thongTinTienTrinh = new ProcessStartInfo
                {
                    FileName = _duongDanSoffice,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                thongTinTienTrinh.ArgumentList.Add("--headless");
                thongTinTienTrinh.ArgumentList.Add("--norestore");
                thongTinTienTrinh.ArgumentList.Add($"-env:UserInstallation=file:///{duongDanProfile}");
                thongTinTienTrinh.ArgumentList.Add("--convert-to");
                thongTinTienTrinh.ArgumentList.Add("pdf");
                thongTinTienTrinh.ArgumentList.Add("--outdir");
                thongTinTienTrinh.ArgumentList.Add(thuMucLamViec);
                thongTinTienTrinh.ArgumentList.Add(duongDanDocx);

                using var tienTrinh = Process.Start(thongTinTienTrinh)
                    ?? throw new ApiException("Không khởi động được LibreOffice để chuyển PDF.", StatusCodes.Status500InternalServerError);

                var daHoanThanh = await Task.Run(() => tienTrinh.WaitForExit(_thoiGianChoToiDaGiay * 1000));
                if (!daHoanThanh)
                {
                    try { tienTrinh.Kill(entireProcessTree: true); } catch { /* best-effort */ }
                    throw new ApiException("Chuyển đổi PDF quá thời gian chờ.", StatusCodes.Status500InternalServerError);
                }

                if (tienTrinh.ExitCode != 0 || !File.Exists(duongDanPdf))
                {
                    var noiDungLoi = await tienTrinh.StandardError.ReadToEndAsync();
                    throw new ApiException(
                        $"Chuyển đổi PDF thất bại (LibreOffice thoát mã {tienTrinh.ExitCode}). " +
                        "Kiểm tra server đã cài LibreOffice và cấu hình LibreOffice:SofficePath đúng đường dẫn chưa." +
                        (string.IsNullOrWhiteSpace(noiDungLoi) ? "" : $" Chi tiết: {noiDungLoi}"),
                        StatusCodes.Status500InternalServerError);
                }

                return await File.ReadAllBytesAsync(duongDanPdf);
            }
            catch (Win32Exception)
            {
                // Lỗi phổ biến nhất: không tìm thấy file thực thi soffice (chưa
                // cài LibreOffice hoặc sai đường dẫn cấu hình).
                throw new ApiException(
                    $"Không tìm thấy LibreOffice tại '{_duongDanSoffice}'. Cài LibreOffice trên server hoặc cấu hình LibreOffice:SofficePath trong appsettings.json.",
                    StatusCodes.Status500InternalServerError);
            }
            finally
            {
                try { Directory.Delete(thuMucLamViec, recursive: true); } catch { /* best-effort dọn dẹp */ }
            }
        }
    }
}
