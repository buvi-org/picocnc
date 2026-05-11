using PicoCNCWeb.Services;
using PicoCNCWeb.Api;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<CncBuilder>();

var app = builder.Build();

// WebSocket support for build progress streaming
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Static files from wwwroot
app.UseStaticFiles();

// API endpoints
app.MapConfigEndpoints();
app.MapPresetEndpoints();

var cncBuilder = app.Services.GetRequiredService<CncBuilder>();
app.MapBuildEndpoints(cncBuilder);
app.MapStlEndpoints();

app.MapFallbackToFile("index.html");

Console.WriteLine("============================================");
Console.WriteLine("  PicoCNC Web Maker");
Console.WriteLine("  Open http://localhost:5176 in a browser");
Console.WriteLine("============================================");

app.Run();
