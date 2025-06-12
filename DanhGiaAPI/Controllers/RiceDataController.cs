using DanhGiaAPI.Models;
using Microsoft.AspNetCore.Mvc;
using static System.Net.WebRequestMethods;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using Tool_NhaAn.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DanhGiaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RiceDataController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public RiceDataController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        // GET: api/<RiceDataController>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<DuLieuCom>>> GetDatas([FromQuery] KetQuaFilterParameters filter)
        {
            if(filter == null) { return NotFound(); }
            var source = _context.DuLieuCom.Where(x => x.Ngay >= filter.TuNgay.Value.Date && x.Ngay <= filter.DenNgay.Value.Date);
            var totalCount = await source.CountAsync();

            var data = await source
                .OrderByDescending(x => x.Ngay)
                .ToListAsync();

            return Ok(new PagedResponse<DuLieuCom>(data, totalCount));
        }

        // GET api/<RiceDataController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<RiceDataController>
        [HttpPost("SyncData")]
        public async Task<ActionResult<DuLieuCom>> SyncData([FromBody] KetQuaFilterParameters filter)
        {
            if(filter == null) { return NotFound(); }
            if(filter.TuNgay != null && filter.DenNgay != null)
            {
                DateTime startDate = (DateTime)filter.TuNgay;
                DateTime endDate = (DateTime)filter.DenNgay;
                var diaDiems = _context.DiaDiemNhaAn.Where(x => x.IsActive).ToList();
                var buaAn =  _context.BuaAn.ToList();
                string apiUrl = _config["Api:Url"];
                string token = _config["Api:BearerToken"];
                for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    foreach (var dd in diaDiems)
                    {
                        // Ví dụ dữ liệu truyền lên API
                        var MealDt = date.ToString("yyyyMMdd");
                        var BepAn = "ALL";
                        var NhaAn = dd.CodeDiemAn;
                        foreach (var ba in buaAn)
                        {
                            var BuaAn = ba.CodeBuaAn;
                            var urlWithParams = $"{apiUrl}?MealDt={MealDt}&BepAn={BepAn}&NhaAn={NhaAn}&BuaAn={BuaAn}";
                            try
                            {
                                using var client = new HttpClient();
                                var request = new HttpRequestMessage(HttpMethod.Get, urlWithParams);
                                request.Headers.Add("Authorization", token);

                                // Gửi request
                                var response = await client.SendAsync(request);
                                response.EnsureSuccessStatusCode(); // Optional: throw nếu HTTP status không thành công

                                // Đọc nội dung response
                                var result = await response.Content.ReadAsStringAsync();

                                // Giải mã JSON thành object
                                var apiResult = JsonSerializer.Deserialize<ApiResponse>(result);

                                if (apiResult?.result == "S" && apiResult.data != null)
                                {
                                    foreach (var item in apiResult.data)
                                    {
                                        //Console.WriteLine($"Đăng ký: {item.coM_DANGKY}, Thực tế: {item.coM_THUCTE}");

                                        // Ghi vào SQL Server
                                        var insert = new DuLieuCom
                                        {
                                            ID_DiemAn = dd.ID,
                                            CodeDiemAn = dd.CodeDiemAn,
                                            Ngay = date.Date,
                                            Com_DangKy_ALL = ba.CodeBuaAn == "ALL" ? (int)item.coM_DANGKY : null,
                                            Com_ThucTe_ALL = ba.CodeBuaAn == "ALL" ? (int)item.coM_THUCTE : null,
                                            Com_DangKy_Sang = ba.CodeBuaAn == "01" ? (int)item.coM_DANGKY : null,
                                            Com_ThucTe_Sang = ba.CodeBuaAn == "01" ? (int)item.coM_THUCTE : null,
                                            Com_DangKy_Trua = ba.CodeBuaAn == "02" ? (int)item.coM_DANGKY : null,
                                            Com_ThucTe_Trua = ba.CodeBuaAn == "02" ? (int)item.coM_THUCTE : null,
                                            Com_DangKy_Chieu = ba.CodeBuaAn == "03" ? (int)item.coM_DANGKY : null,
                                            Com_ThucTe_Chieu = ba.CodeBuaAn == "03" ? (int)item.coM_THUCTE : null,
                                            Com_DangKy_Dem = ba.CodeBuaAn == "04" ? (int)item.coM_DANGKY : null,
                                            Com_ThucTe_Dem = ba.CodeBuaAn == "04" ? (int)item.coM_THUCTE : null,
                                        };
                                        var checkDL = await _context.DuLieuCom.Where(x => x.Ngay == date.Date && x.ID_DiemAn == dd.ID && x.CodeDiemAn == NhaAn).FirstOrDefaultAsync();
                                        if (checkDL != null)
                                        {
                                            //update
                                            if (ba.CodeBuaAn == "01")
                                            {
                                                checkDL.Com_ThucTe_Sang = (int)item.coM_THUCTE;
                                                checkDL.Com_DangKy_Sang = (int)item.coM_DANGKY;
                                            }
                                            else if (ba.CodeBuaAn == "02")
                                            {
                                                checkDL.Com_ThucTe_Trua = (int)item.coM_THUCTE;
                                                checkDL.Com_DangKy_Trua = (int)item.coM_DANGKY;
                                            }
                                            else if (ba.CodeBuaAn == "03")
                                            {
                                                checkDL.Com_ThucTe_Chieu = (int)item.coM_THUCTE;
                                                checkDL.Com_DangKy_Chieu = (int)item.coM_DANGKY;
                                            }
                                            else if (ba.CodeBuaAn == "04")
                                            {
                                                checkDL.Com_ThucTe_Dem = (int)item.coM_THUCTE;
                                                checkDL.Com_DangKy_Dem = (int)item.coM_DANGKY;
                                            }
                                            else if (ba.CodeBuaAn == "ALL")
                                            {
                                                checkDL.Com_ThucTe_ALL = (int)item.coM_THUCTE;
                                                checkDL.Com_DangKy_ALL = (int)item.coM_DANGKY;
                                            }
                                            await _context.SaveChangesAsync();
                                        }
                                        else
                                        {
                                            _context.DuLieuCom.Add(insert);
                                        }
                                        //WriteLog(logPath, $"✔ Thành công: {ba.TenBuaAn} - Đăng ký:  {item.coM_DANGKY} - Thực tế: {item.coM_THUCTE}");
                                    }

                                    await _context.SaveChangesAsync();

                                }
                                else
                                {
                                    return StatusCode(500, "API trả về lỗi hoặc không có dữ liệu.");
                                }
                            }
                            catch (Exception ex)
                            {
                                return StatusCode(500, $" Lỗi ghi DB cho {dd.DiaDiem} : {ex.Message}");
                            }
                        }
                    }

                }
            }
            return StatusCode(201, "Đồng bộ dữ liệu thành công");
        }

        // PUT api/<RiceDataController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<RiceDataController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
