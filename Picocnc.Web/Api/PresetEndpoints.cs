using PicoGK;

namespace PicoCNCWeb.Api;

public static class PresetEndpoints
{
    public static void MapPresetEndpoints(this WebApplication app)
    {
        app.MapGet("/api/presets", () =>
        {
            return Results.Ok(Picocnc.aPresets);
        });

        app.MapPost("/api/preset/{name}", (string name) =>
        {
            var preset = Picocnc.aPresets.FirstOrDefault(
                p => p.strName.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (preset.strName is null)
                return Results.NotFound(new { error = $"Preset not found: {name}" });

            Picocnc.ApplyPreset(preset);
            return Results.Ok(new { status = "ok", preset = preset.strName });
        });
    }
}
