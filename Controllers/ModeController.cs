using Microsoft.AspNetCore.Mvc;

namespace MultiplicationGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModeController : ControllerBase
    {
        private readonly ILogger<ModeController> _logger;

        public ModeController(ILogger<ModeController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult SetMode([FromBody] System.Collections.Generic.Dictionary<string, string>? payload)
        {
            _logger.LogInformation(
                "SetMode called: HasPayload={HasPayload}, IP={IP}",
                payload != null,
                HttpContext.Connection.RemoteIpAddress);

            if (payload == null)
            {
                _logger.LogWarning("SetMode called with null payload");
                return BadRequest(new { error = "Mode is required" });
            }

            // Accept either 'mode' or 'Mode' keys to be tolerant to client payload casing
            payload.TryGetValue("mode", out var modeValLower);
            payload.TryGetValue("Mode", out var modeValUpper);
            var modeVal = modeValUpper ?? modeValLower;

            if (string.IsNullOrWhiteSpace(modeVal))
            {
                _logger.LogWarning("SetMode called with empty mode value");
                return BadRequest(new { error = "Mode is required" });
            }

            // validate and accept only Normal and Learning modes
            if (!Enum.TryParse<Pages.IndexModel.LearningMode>(modeVal, true, out var parsed))
            {
                _logger.LogWarning("Invalid mode value: {Mode}", modeVal);
                return BadRequest(new { error = "Invalid mode" });
            }
            
            if (parsed != Pages.IndexModel.LearningMode.Normal && parsed != Pages.IndexModel.LearningMode.Learning)
            {
                _logger.LogWarning("Unsupported mode: {Mode}", parsed);
                return BadRequest(new { error = "Invalid mode" });
            }

            // set a cookie so subsequent requests see the mode
            Response.Cookies.Append("mg_mode", modeVal, new()
            {
                HttpOnly = false,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            _logger.LogInformation("Mode set successfully: {Mode}", modeVal);

            return Ok(new { mode = modeVal });
        }
    }
}
