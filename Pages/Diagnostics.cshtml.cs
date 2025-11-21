using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using MultiplicationGame.Services;

namespace MultiplicationGame.Pages;

public sealed class DiagnosticsModel : PageModel
{
    private readonly GameSettings? _settings;
    private readonly IHostEnvironment _env;

    public DiagnosticsModel(IOptions<GameSettings> opt, IHostEnvironment env)
    {
        _settings = opt?.Value;
        _env = env;
    }

    public GameSettings? Settings => _settings;
    public string EnvironmentName => _env.EnvironmentName;
    public string ContentRoot => _env.ContentRootPath;

    public void OnGet() { /* no-op */ }
    public void OnPost() { /* Refresh simply re-renders */ }
}
