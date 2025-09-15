using Microsoft.AspNetCore.Mvc;

namespace MultiplicationGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModeController : ControllerBase
    {
        public record ModeRequest(string Mode);

        [HttpPost]
        public IActionResult SetMode([FromBody] ModeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Mode))
                return BadRequest(new { error = "Mode is required" });

            // validate against known modes
            if (!Enum.TryParse<Pages.IndexModel.LearningMode>(request.Mode, true, out var _))
                return BadRequest(new { error = "Invalid mode" });

            // set a cookie so subsequent requests see the mode
            Response.Cookies.Append("mg_mode", request.Mode, new()
            {
                HttpOnly = false,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return Ok(new { mode = request.Mode });
        }
    }
}
