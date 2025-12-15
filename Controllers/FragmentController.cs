using Microsoft.AspNetCore.Mvc;

namespace MultiplicationGame.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FragmentController : ControllerBase
{
    private readonly ILogger<FragmentController> _logger;

    public FragmentController(ILogger<FragmentController> logger)
    {
        _logger = logger;
    }

    // Learning-mode dynamic fragment endpoint removed. Keep 410 for stale clients.
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("/api/fragment called but learning mode is removed");
        return StatusCode(410, new { error = "Learning mode removed" });
    }
}
