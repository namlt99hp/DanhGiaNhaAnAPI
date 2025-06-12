using DanhGiaAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DanhGiaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeFrameController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TimeFrameController(AppDbContext context)
        {
            _context = context;
        }
        // GET: api/<TimeFrameController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<KhungGioDanhGia>>> Get()
        {
            //var kq = await _context.KhungGioDanhGia.ToListAsync();
            return Ok();
        }

        // GET api/<TimeFrameController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<TimeFrameController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<TimeFrameController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<TimeFrameController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
