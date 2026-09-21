using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DanhGiaAPI.Controllers
{
    // Chuyển đổi file dùng chung cho Phiếu 1-4 — hiện chỉ có docx -> pdf (xem
    // ChuyenDoiFileService, cần LibreOffice cài trên server).
    [Authorize]
    [Route("api/chuyen-doi")]
    [ApiController]
    public class ChuyenDoiFileController : ControllerBase
    {
        private readonly IChuyenDoiFileService _chuyenDoiFileService;

        public ChuyenDoiFileController(IChuyenDoiFileService chuyenDoiFileService)
        {
            _chuyenDoiFileService = chuyenDoiFileService;
        }

        // POST api/chuyen-doi/docx-sang-pdf (multipart/form-data: file)
        // FE tự sinh file .docx bằng thư viện "docx" (xem xuatWordPhieu1.ts/2.ts)
        // rồi gửi nguyên buffer lên đây để convert, thay vì viết lại layout
        // phiếu riêng cho PDF.
        [HttpPost("docx-sang-pdf")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> DocxSangPdf(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var pdfBytes = await _chuyenDoiFileService.DocxSangPdfAsync(ms.ToArray());

            var tenFilePdf = Path.ChangeExtension(Path.GetFileName(file.FileName), "pdf");
            return File(pdfBytes, "application/pdf", tenFilePdf);
        }
    }
}
