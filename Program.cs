using nav2wpt;

if (args.Length != 4)
{
    Console.WriteLine("Usage:");
    Console.WriteLine($"\t{nameof(nav2wpt)} <NAV dir> <BSP dir> <TYPE> <WPT dir>");
    Console.WriteLine("Examples:");
    Console.WriteLine($"\t{nameof(nav2wpt)} D:\\hlds\\czero\\maps D:\\hlds\\cstrike\\maps FoXBot D:\\hlds\\tfc\\addons\\foxbot\\tfc\\waypoints");
    Console.WriteLine($"\t{nameof(nav2wpt)} D:\\hlds\\czero\\maps D:\\hlds\\cstrike\\maps sandbot D:\\hlds\\valve\\maps");
    return;
}

var navDir = args[0];
var bspDir = args[1];
var type = args[2];
var wptDir = args[3];

foreach (var navFile in Directory.GetFiles(navDir).Where(f => Path.GetExtension(f) == ".nav"))
{
    var bspFile = Path.Combine(bspDir, Path.GetFileNameWithoutExtension(navFile) + ".bsp");
    if (!File.Exists(bspFile))
        continue;

    WaypointFile writer = type.ToLowerInvariant() switch
    {
        "foxbot" => new FoxbotFile(),
        "sandbot" => new SandbotFile(),
        _ => throw new InvalidOperationException("Invalid waypoint file type!")
    };

    var waypointFile = Path.Combine(wptDir, Path.GetFileNameWithoutExtension(navFile) + writer.Extension);
    if (File.Exists(waypointFile))
    {
        var authorBytes = new byte[255];
        using var wf = File.OpenRead(waypointFile);
        wf.Position = wf.Length - authorBytes.Length;
        wf.Read(authorBytes);
        var author = System.Text.Encoding.ASCII.GetString(authorBytes.TakeWhile(b => b != 0).ToArray());
        if (author == WaypointFile.Author)
            continue;
    }
    Console.WriteLine($"Converting {navFile} to {waypointFile}");
    var waypoints = Converter.GetWaypoints(navFile, bspFile);
    writer.Write(waypointFile, waypoints);
}
Console.WriteLine("Done!");
