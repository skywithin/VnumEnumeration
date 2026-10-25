// Prints the next sequential backlog ID for a prefix.
// Usage: dotnet run tools/backlog-next-id.cs <prefix>
// Example: dotnet run tools/backlog-next-id.cs BL     -> BL-0013
//          dotnet run tools/backlog-next-id.cs BUG    -> BUG-0003

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: dotnet run tools/backlog-next-id.cs <prefix>");
    Console.Error.WriteLine("Example: dotnet run tools/backlog-next-id.cs BL");
    return 1;
}

var prefix = args[0].ToUpperInvariant();

var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
var itemsDir = Path.Combine(repoRoot, "backlog", "items");

if (!Directory.Exists(itemsDir))
{
    Console.Error.WriteLine($"Items directory not found: {itemsDir}");
    return 1;
}

var maxId = 0;
var pattern = $"{prefix}-";

foreach (var file in Directory.GetFiles(itemsDir, $"{prefix}-*.md"))
{
    var fileName = Path.GetFileNameWithoutExtension(file);
    var numPart = fileName[pattern.Length..];
    if (int.TryParse(numPart, out var num) && num > maxId)
        maxId = num;
}

Console.WriteLine($"{prefix}-{(maxId + 1):D4}");
return 0;

string FindRepoRoot(string startDir)
{
    var dir = startDir;
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir, ".git")))
            return dir;
        dir = Path.GetDirectoryName(dir);
    }
    return startDir;
}
