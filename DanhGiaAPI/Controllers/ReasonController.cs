using DanhGiaAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DanhGiaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReasonController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ReasonController(AppDbContext context)
        {
            _context = context;
        }
        // GET: api/<ReasonController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LyDo>>> Get()
        {
            //var kq = await _context.LyDo.ToListAsync();
            //return Ok(kq);
            return Ok();
        }

        // GET api/<ReasonController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ReasonController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ReasonController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ReasonController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
