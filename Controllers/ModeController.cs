using Microsoft.AspNetCore.Mvc;

namespace MultiplicationGame.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModeController : ControllerBase
{
    private readonly ILogger<ModeController> _logger;

    public ModeController(ILogger<ModeController> logger)
    {
        _logger = logger;
    }

    // Learning mode has been removed. Keep endpoint to avoid 404s in stale clients.
    [HttpPost]
    public IActionResult SetMode()
    {
        _logger.LogInformation("/api/mode called but learning mode is removed");
        return StatusCode(410, new { error = "Learning mode removed" });
    }
}
