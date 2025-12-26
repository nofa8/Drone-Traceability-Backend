using Microsoft.AspNetCore.Mvc;

namespace dTITAN.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController] // (automatic model binding, validation, etc.)
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Backend running with MongoDB + Redis");
        }
    }
}


