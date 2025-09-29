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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();


app.MapStaticAssets();
app.MapControllers();
app.MapRazorPages().WithStaticAssets();

await app.RunAsync();
