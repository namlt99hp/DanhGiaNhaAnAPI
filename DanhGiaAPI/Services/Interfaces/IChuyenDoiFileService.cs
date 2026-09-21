namespace DanhGiaAPI.Services.Interfaces
{
    // Chuyển đổi file dùng chung cho Phiếu 1-4 — hiện chỉ có docx -> pdf, tận
    // dụng nguyên file .docx do FE tự sinh (xem xuatWordPhieu1.ts/2.ts) qua
    // LibreOffice headless thay vì viết lại layout phiếu riêng cho PDF.
    public interface IChuyenDoiFileService
    {
        Task<byte[]> DocxSangPdfAsync(byte[] docxBytes);
    }
}
