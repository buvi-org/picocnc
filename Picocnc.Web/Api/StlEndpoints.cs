namespace PicoCNCWeb.Api;

public static class StlEndpoints
{
    public static void MapStlEndpoints(this WebApplication app)
    {
        app.MapGet("/api/stl/{component}", (string component) =>
        {
            string strPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PicoCNC_Web", $"{component}.stl");

            if (!File.Exists(strPath))
                return Results.NotFound(new { error = $"STL not found: {component}" });

            return Results.File(strPath, "application/sla", $"{component}.stl");
        });
    }
}
