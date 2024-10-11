using Microsoft.AspNetCore.Mvc;

namespace ArtworkCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WelcomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new {message = "This is backend site of my Artwork page!"});
        }
    }
}
