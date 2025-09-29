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

        public FragmentController(IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider, Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _antiforgery = antiforgery;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? mode)
        {
            // Create a fresh PageModel to pass to the partial so it renders consistently.
            var gameService = _serviceProvider.GetRequiredService<MultiplicationGame.Services.IGameService>();
            var gameStateService = _serviceProvider.GetRequiredService<MultiplicationGame.Services.IGameStateService>();
            var model = new MultiplicationGame.Pages.IndexModel(gameService, gameStateService);

            // If mode query provided, set model.Mode accordingly
            if (!string.IsNullOrEmpty(mode) && System.Enum.TryParse(mode, out MultiplicationGame.Pages.IndexModel.LearningMode parsed))
            {
                if (parsed == MultiplicationGame.Pages.IndexModel.LearningMode.Normal || parsed == MultiplicationGame.Pages.IndexModel.LearningMode.Learning)
                {
                    model.Mode = parsed;
                }
            }
            else if (HttpContext.Request.Cookies.TryGetValue("mg_mode", out var cookieMode) && System.Enum.TryParse(cookieMode, out MultiplicationGame.Pages.IndexModel.LearningMode cookieParsed))
            {
                model.Mode = cookieParsed;
            }

            var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);
            using var sw = new StringWriter();

            var viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: "~/Pages/Shared/_InteractiveFragment.cshtml", isMainPage: false);
            if (!viewResult.Success)
            {
                return StatusCode(500, new { error = "Interactive fragment view not found" });
            }

            // If the fragment is requested in Learning mode, ensure there's an active question so
            // the learning UI (addition steps, dot groups, auto-advance) is rendered in the partial.
            if (model.Mode == MultiplicationGame.Pages.IndexModel.LearningMode.Learning)
            {
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
                    var question = gameService.GetQuestion(level: model.Level, solvedQuestions: model.SolvedQuestions ?? string.Empty);
                    if (question != null)
                    {
                        model.Question = question;
                        model.A = question.A;
                        model.B = question.B;
                        model.Level = question.Level;
                    }
                }
                catch
                {
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
            catch { /* swallow; fragment rendering should still proceed */ }

            await viewResult.View.RenderAsync(viewContext);

            var rendered = sw.ToString();
            return Content(rendered, "text/html; charset=utf-8");
        }
    }
}
