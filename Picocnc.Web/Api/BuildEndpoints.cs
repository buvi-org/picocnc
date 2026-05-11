using System.Net.WebSockets;
using PicoCNCWeb.Services;

namespace PicoCNCWeb.Api;

public static class BuildEndpoints
{
    public static void MapBuildEndpoints(this WebApplication app, CncBuilder builder)
    {
        // WebSocket endpoint — streams progress events in real time
        app.MapGet("/api/build/ws", async (HttpContext ctx) =>
        {
            if (!ctx.WebSockets.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.WriteAsync("WebSocket connection required");
                return;
            }

            using var ws = await ctx.WebSockets.AcceptWebSocketAsync();

            // Read optional voxel override from query string
            float? fVoxelOverride = null;
            if (ctx.Request.Query.TryGetValue("voxelSize", out var vs) &&
                float.TryParse(vs, out var parsed))
            {
                fVoxelOverride = parsed;
            }

            await builder.BuildAndStreamAsync(ws, fVoxelOverride);

            // Close the WebSocket gracefully
            if (ws.State == WebSocketState.Open)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Build complete", CancellationToken.None);
            }
        });
    }
}
