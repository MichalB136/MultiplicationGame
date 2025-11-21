using MultiplicationGame.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
// HttpClient factory for internal fragment fetching
builder.Services.AddHttpClient();

// Register game services
// Bind GameSettings from configuration
builder.Services.Configure<GameSettings>(builder.Configuration.GetSection("GameSettings"));

// Register game services
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGameStateService, GameStateService>();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting in {Environment} environment", app.Environment.EnvironmentName);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files directly from wwwroot
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

logger.LogInformation("Application configured and ready to accept requests");

await app.RunAsync();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
