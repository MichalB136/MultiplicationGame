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

        public FragmentController(IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
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
                model.Mode = parsed;
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

            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };

            var tempData = new TempDataDictionary(HttpContext, _tempDataProvider);

            var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary, tempData, sw, new HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);

            var rendered = sw.ToString();
            return Content(rendered, "text/html");
        }
    }
}
