using DanhGiaAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DanhGiaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EvaluatesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public EvaluatesController(AppDbContext context)
        {
            _context = context;
        }
        // GET: api/<Evaluates>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<KetQuaDanhGia>>> GetDatas([FromQuery] KetQuaFilterParameters filter)
        {
            var khunggio = _context.KhungGioDanhGia.ToList();
            var allowedTimeRanges = new List<(TimeSpan start, TimeSpan end)> { };
            foreach (var item in khunggio)
            {
                allowedTimeRanges.Add(((TimeSpan)item.TuGio, (TimeSpan)item.DenGio));
            }

            var source = from o in _context.KetQuaDanhGia.Where(x=>x.ThoiGianDanhGia >= filter.TuNgay && x.ThoiGianDanhGia <= filter.DenNgay)
                         join c in _context.DiaDiemNhaAn on o.DiaDiem_ID equals c.ID
                         join ba in _context.BuaAn on o.ID_BuaAn equals ba.ID
                         select new KetQuaDanhGiaDTO
                         {
                             ID = o.ID,
                             DiaDiem_ID = o.DiaDiem_ID,
                             TenDiaDiem = c.DiaDiem,
                             DiemDanhGia = o.DiemDanhGia,
                             ThoiGianDanhGia = o.ThoiGianDanhGia,
                             ID_BuaAn = o.ID_BuaAn,
                             CodeBuaAn = ba.CodeBuaAn
                         };

            

            if (filter.CodeBuaAn == "01" )
            {
                var from = (TimeSpan)khunggio.FirstOrDefault(x=>x.ID ==1).TuGio;
                var to = (TimeSpan)khunggio.FirstOrDefault(x => x.ID == 1).DenGio;
                source = source.Where(x => x.ThoiGianDanhGia.TimeOfDay >= from && x.ThoiGianDanhGia.TimeOfDay <= to);
            }
            else if (filter.CodeBuaAn == "02")
            {
                var from = (TimeSpan)khunggio.FirstOrDefault(x => x.ID == 2).TuGio;
                var to = (TimeSpan)khunggio.FirstOrDefault(x => x.ID == 2).DenGio;
                source = source.Where(x => x.ThoiGianDanhGia.TimeOfDay >= from && x.ThoiGianDanhGia.TimeOfDay <= to);
            }
            else if (filter.CodeBuaAn == "03")
            {
                var from = (TimeSpan)khunggio.FirstOrDefault(x => x.ID == 3).TuGio;
                var to = (TimeSpan)khunggio.FirstOrDefault(x => x.ID == 3).DenGio;
                source = source.Where(x => x.ThoiGianDanhGia.TimeOfDay >= from && x.ThoiGianDanhGia.TimeOfDay <= to);
            }
            else if (filter.CodeBuaAn == "04")
            {
                var from = (TimeSpan)khunggio.FirstOrDefault(x => x.ID == 4).TuGio;
                var to = (TimeSpan)khunggio.FirstOrDefault(x => x.ID == 4).DenGio;
                source = source.Where(x => x.ThoiGianDanhGia.TimeOfDay >= from && x.ThoiGianDanhGia.TimeOfDay <= to);
            }
            // Optional: thêm filter
            // source = source.Where(x => x.CustomerName.Contains("John"));

            //var count = await source.CountAsync();

            //var items = await source
            //    .OrderBy(p => p.ThoiGianDanhGia)
            //    .Skip((pagingParams.PageNumber - 1) * pagingParams.PageSize)
            //    .Take(pagingParams.PageSize)
            //    .ToListAsync();

            //var response = new PagedResponse<KetQuaDanhGiaDTO>(items, count, pagingParams.PageNumber, pagingParams.PageSize);
            var totalCount = await source.CountAsync();

            var data = await source
                .OrderByDescending(x => x.ThoiGianDanhGia)
                .ToListAsync();

            return Ok(new PagedResponse<KetQuaDanhGiaDTO>(data, totalCount));
        }

        // GET api/<Evaluates>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<Evaluates>
        [HttpPost]
        public async Task<ActionResult<KetQuaDanhGia>> Post(KetQuaDanhGia ketqua)
        {
           var khunggio =  _context.KhungGioDanhGia.ToList();
           var now = DateTime.Now.TimeOfDay;
            
            if (ketqua == null || ketqua.DiaDiem_ID == 0 ) { return NotFound(); }
            var checkbuaan = khunggio.FirstOrDefault(x => now >= x.TuGio && now <= x.DenGio);
            if (checkbuaan == null)
            {
                return Created("", null);
            }
            KetQuaDanhGia kq = new KetQuaDanhGia()
            {
                DiaDiem_ID = ketqua.DiaDiem_ID,
                DiemDanhGia = ketqua.DiemDanhGia,
                ThoiGianDanhGia = DateTime.Now,
                TieuChi_ID =1, // thiết lập tiêu chí mặc định
                ID_BuaAn = checkbuaan?.ID??0
                //ID_KhungGio = ketqua.ID_KhungGio,
                //ID_LyDo = ketqua.ID_LyDo,
            };
            _context.KetQuaDanhGia.Add(kq);
            await _context.SaveChangesAsync();
            return Created("", kq);
        }

        // PUT api/<Evaluates>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<Evaluates>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
        [HttpGet("export-excel")]
        public IActionResult ExportExcel([FromQuery] KetQuaFilterParameters filter)
        {
            var stream = new MemoryStream();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("Du lieu khao sat danh gia");
                // Header
                worksheet.Cells[1, 1].Value = "STT";
                worksheet.Cells[1, 2].Value = "Nhà ăn";
                worksheet.Cells[1, 3].Value = "Điểm đánh giá";
                worksheet.Cells[1, 4].Value = "Ngày đánh giá";

                // Data
                var source = (from o in _context.KetQuaDanhGia.Where(x => x.ThoiGianDanhGia >= filter.TuNgay && x.ThoiGianDanhGia <= filter.DenNgay)
                             join c in _context.DiaDiemNhaAn on o.DiaDiem_ID equals c.ID
                             select new KetQuaDanhGiaDTO
                             {
                                 ID = o.ID,
                                 DiaDiem_ID = o.DiaDiem_ID,
                                 TenDiaDiem = c.DiaDiem,
                                 DiemDanhGia = o.DiemDanhGia,
                                 ThoiGianDanhGia = o.ThoiGianDanhGia
                             }).ToList();
                int row = 2;
                foreach (var item in source)
                {
                    worksheet.Cells[row, 1].Value = row -1;
                    worksheet.Cells[row, 2].Value = item.TenDiaDiem;
                    worksheet.Cells[row, 3].Value = item.DiemDanhGia;
                    worksheet.Cells[row, 4].Value = item.ThoiGianDanhGia.ToString("dd-MM-yyyy hh:mm:ss");
                    row++;
                }



                package.Save();
            }
            stream.Position = 0;
            var fileName = "Du lieu danh gia.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}
