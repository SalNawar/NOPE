using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// The CITY-R17 data as the pure tests read it: the exported scene, traffic, fleet and art map
/// (<c>Assets/Data/City/R17</c>) and the r17 page's golden record (<c>Assets/Tests/EditMode/City/R17/Data</c>), each
/// parsed once. Paths resolve from the project folder (Unity) or from this file (the offline runner).
/// </summary>
public static class CityR17Fixture
{
    private static CityR17Scene _scene;
    private static CityR17TrafficData _traffic;
    private static CityR17Models _models;
    private static CityR17Art _art;
    private static ContentNode _golden;

    public static CityR17Scene Scene => _scene ??= CityR17Scene.Parse(Read("Assets/Data/City/R17/city_r17_scene.json"));

    public static CityR17TrafficData Traffic => _traffic ??= CityR17TrafficData.Parse(Read("Assets/Data/City/R17/city_r17_traffic.json"));

    /// <summary>A fresh fleet (DropPods edits the arrays, so each caller that inspects raw arrays parses its own).</summary>
    public static CityR17Models Models => _models ??= CityR17Models.Parse(Read("Assets/Data/City/R17/city_r17_vehicles.json"));

    public static CityR17Art Art => _art ??= CityR17Art.Parse(Read("Assets/Data/City/R17/city_r17_art.json"));

    /// <summary>The page's golden record.</summary>
    public static ContentNode Golden => _golden ??= ContentJson.Parse(Read("Assets/Tests/EditMode/City/R17/Data/city_r17_golden.json"));

    /// <summary>A project file's text.</summary>
    public static string Read(string projectPath, [CallerFilePath] string here = "")
    {
        if (File.Exists(projectPath))
            return File.ReadAllText(projectPath);
        string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(root, projectPath));
    }

    /// <summary>Whether a project file exists (Unity's working folder or the offline runner's).</summary>
    public static bool Exists(string projectPath, [CallerFilePath] string here = "")
    {
        if (File.Exists(projectPath))
            return true;
        string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", "..", ".."));
        return File.Exists(Path.Combine(root, projectPath));
    }

    /// <summary>A sim on the page's data, prefilled and settled as the page installs it.</summary>
    public static CityR17Traffic NewSim()
    {
        var sim = new CityR17Traffic(Traffic, Models) { PlateW = Scene.PlateW, PlateH = Scene.PlateH };
        sim.Prefill();
        sim.Settle(0.7294766865079365);
        return sim;
    }
}
