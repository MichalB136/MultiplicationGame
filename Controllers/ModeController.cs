using Microsoft.AspNetCore.Mvc;

namespace MultiplicationGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModeController : ControllerBase
    {
        [HttpPost]
        public IActionResult SetMode([FromBody] System.Collections.Generic.Dictionary<string, string>? payload)
        {
            if (payload == null)
                return BadRequest(new { error = "Mode is required" });

            // Accept either 'mode' or 'Mode' keys to be tolerant to client payload casing
            payload.TryGetValue("mode", out var modeValLower);
            payload.TryGetValue("Mode", out var modeValUpper);
            var modeVal = modeValUpper ?? modeValLower;

            if (string.IsNullOrWhiteSpace(modeVal))
                return BadRequest(new { error = "Mode is required" });

            // validate against known modes
            if (!Enum.TryParse<Pages.IndexModel.LearningMode>(modeVal, true, out var _))
                return BadRequest(new { error = "Invalid mode" });

            // set a cookie so subsequent requests see the mode
            Response.Cookies.Append("mg_mode", modeVal, new()
            {
                HttpOnly = false,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return Ok(new { mode = modeVal });
        }
    }
}
