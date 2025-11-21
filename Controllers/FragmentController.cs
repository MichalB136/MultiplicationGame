using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MultiplicationGame.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FragmentController : ControllerBase
    {
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly Microsoft.AspNetCore.Antiforgery.IAntiforgery _antiforgery;
    private readonly ILogger<FragmentController> _logger;

        public FragmentController(
            IRazorViewEngine viewEngine, 
            ITempDataProvider tempDataProvider, 
            IServiceProvider serviceProvider, 
            Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery,
            ILogger<FragmentController> logger)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _antiforgery = antiforgery;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? mode)
        {
            _logger.LogInformation(
                "Fragment requested: Mode={Mode}, IP={IP}",
                mode ?? "null",
                HttpContext.Connection.RemoteIpAddress);

            // Create a fresh PageModel to pass to the partial so it renders consistently.
            var gameService = _serviceProvider.GetRequiredService<MultiplicationGame.Services.IGameService>();
            var gameStateService = _serviceProvider.GetRequiredService<MultiplicationGame.Services.IGameStateService>();
            var pageLogger = _serviceProvider.GetService<ILogger<MultiplicationGame.Pages.IndexModel>>();
            var options = _serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<MultiplicationGame.Services.GameSettings>>();
            var model = new MultiplicationGame.Pages.IndexModel(gameService, gameStateService, pageLogger, options);

            // If mode query provided, set model.Mode accordingly
            if (!string.IsNullOrEmpty(mode) && System.Enum.TryParse(mode, out MultiplicationGame.Pages.IndexModel.LearningMode parsed))
            {
                if (parsed == MultiplicationGame.Pages.IndexModel.LearningMode.Normal || parsed == MultiplicationGame.Pages.IndexModel.LearningMode.Learning)
                {
                    model.Mode = parsed;
                    _logger.LogDebug("Mode set from query parameter: {Mode}", parsed);
                }
            }
            else if (HttpContext.Request.Cookies.TryGetValue("mg_mode", out var cookieMode) && System.Enum.TryParse(cookieMode, out MultiplicationGame.Pages.IndexModel.LearningMode cookieParsed))
            {
                model.Mode = cookieParsed;
                _logger.LogDebug("Mode set from cookie: {Mode}", cookieParsed);
            }

            var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);
            using var sw = new StringWriter();

            var viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: "~/Pages/Shared/_InteractiveFragment.cshtml", isMainPage: false);
            if (!viewResult.Success)
            {
                _logger.LogError(
                    "Interactive fragment view not found at path: ~/Pages/Shared/_InteractiveFragment.cshtml");
                return StatusCode(500, new { error = "Interactive fragment view not found" });
            }

            // If the fragment is requested in Learning mode, ensure there's an active question so
            // the learning UI (addition steps, dot groups, auto-advance) is rendered in the partial.
            if (model.Mode == MultiplicationGame.Pages.IndexModel.LearningMode.Learning)
            {
                _logger.LogDebug("Initializing Learning mode with question for level {Level}", model.Level);
                
                // Use the model's configured level instead of a hard-coded value so Learning mode
                // behaves consistently with the chosen difficulty.
                var question = gameService.GetQuestion(level: model.Level, solvedQuestions: string.Empty);
                model.Question = question;
                model.A = question.A;
                model.B = question.B;
                model.Level = question.Level;
                model.LearningStep = 0;
            }

            // For other modes, ensure there's at least one question populated so the
            // standard/normal UI renders (otherwise the partial only shows the progress panel).
            if (model.Question == null)
            {
                try
                {
                    _logger.LogDebug("Fetching initial question for level {Level}", model.Level);
                    var question = gameService.GetQuestion(level: model.Level, solvedQuestions: model.SolvedQuestions ?? string.Empty);
                    if (question != null)
                    {
                        model.Question = question;
                        model.A = question.A;
                        model.B = question.B;
                        model.Level = question.Level;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to fetch initial question for level {Level}",
                        model.Level);
                    // Swallow errors to avoid fragment rendering failure; progress panel fallback will still show.
                }
            }

            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };

            var tempData = new TempDataDictionary(HttpContext, _tempDataProvider);

            var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary, tempData, sw, new HtmlHelperOptions());

            // Ensure antiforgery tokens are generated and stored as cookies in the response
            try
            {
                _antiforgery?.GetAndStoreTokens(HttpContext);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate antiforgery tokens");
                /* swallow; fragment rendering should still proceed */
            }

            await viewResult.View.RenderAsync(viewContext);

            var rendered = sw.ToString();
            
            _logger.LogInformation(
                "Fragment rendered successfully: Mode={Mode}, HtmlLength={Length}",
                model.Mode,
                rendered.Length);
            
            return Content(rendered, "text/html; charset=utf-8");
        }
    }
}
