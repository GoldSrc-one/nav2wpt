using nav2wpt;

if (args.Length != 3)
{
    Console.WriteLine("Usage:");
    Console.WriteLine($"\t{nameof(nav2wpt)} <NAV dir> <BSP dir> <FWP dir>");
    Console.WriteLine("Example:");
    Console.WriteLine($"\t{nameof(nav2wpt)} D:\\hlds\\czero\\maps D:\\hlds\\cstrike\\maps D:\\hlds\\tfc\\addons\\foxbot\\tfc\\waypoints");
    return;
}

var navDir = args[0];
var bspDir = args[1];
var fwpDir = args[2];

foreach (var navFile in Directory.GetFiles(navDir).Where(f => Path.GetExtension(f) == ".nav"))
{
    var bspFile = Path.Combine(bspDir, Path.GetFileNameWithoutExtension(navFile) + ".bsp");
    if (!File.Exists(bspFile))
        continue;

    var waypointFile = Path.Combine(fwpDir, Path.GetFileNameWithoutExtension(navFile) + ".fwp");
    if (File.Exists(waypointFile))
    {
        var authorBytes = new byte[255];
        using var wf = File.OpenRead(waypointFile);
        wf.Position = wf.Length - authorBytes.Length;
        wf.Read(authorBytes);
        var author = System.Text.Encoding.ASCII.GetString(authorBytes.TakeWhile(b => b != 0).ToArray());
        if (author == FwpFile.Author)
            continue;
    }
    Console.WriteLine($"Converting {navFile} to {waypointFile}");
    Converter.ConvertToFwp(navFile, bspFile, waypointFile);
}
Console.WriteLine("Done!");
