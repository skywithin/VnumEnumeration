// Updates one frontmatter field in one or more backlog item files, then rebuilds the index.
// Usage: dotnet run tools/backlog-set-field.cs [--dry-run] <ID...> <field> <value>
// Examples:
//   dotnet run tools/backlog-set-field.cs BL-0003 status done
//   dotnet run tools/backlog-set-field.cs BUG-0001 BUG-0002 size S
//   dotnet run tools/backlog-set-field.cs BL-0004..BL-0007 priority low
//   dotnet run tools/backlog-set-field.cs --dry-run BL-0003 status done
//
// Setting status to done/rejected/wont-fix sets `completed` to today; setting it back to
// open/in-progress clears `completed`. Known values are validated (warning only).

var knownValues = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
{
    ["status"]   = ["open", "in-progress", "done", "rejected", "wont-fix"],
    ["priority"] = ["critical", "high", "medium", "low"],
    ["size"]     = ["S", "M", "L", "XL"],
    ["type"]     = ["enhancement", "feature", "tech-debt", "test-gap", "bug", "investigation", "chore"],
    ["area"]     = ["core", "serialization", "tests", "packaging", "docs"],
    ["breaking"] = ["true", "false"],
};

var dryRun = false;
var filteredArgs = new List<string>();
foreach (var arg in args)
{
    if (arg == "--dry-run")
        dryRun = true;
    else
        filteredArgs.Add(arg);
}

if (filteredArgs.Count < 3)
{
    Console.Error.WriteLine("Usage: dotnet run tools/backlog-set-field.cs [--dry-run] <ID...> <field> <value>");
    Console.Error.WriteLine("Examples:");
    Console.Error.WriteLine("  dotnet run tools/backlog-set-field.cs BL-0003 status done");
    Console.Error.WriteLine("  dotnet run tools/backlog-set-field.cs BUG-0001 BUG-0002 size S");
    Console.Error.WriteLine("  dotnet run tools/backlog-set-field.cs BL-0004..BL-0007 priority low");
    return 1;
}

var value = filteredArgs[^1];
var field = filteredArgs[^2];
var idArgs = filteredArgs.GetRange(0, filteredArgs.Count - 2).ToArray();

if (knownValues.TryGetValue(field, out var allowed)
    && !allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Warning: '{value}' is not a known value for '{field}'. Valid: {string.Join(", ", allowed)}");
}

var ids = ExpandIds(idArgs);
if (ids.Count == 0)
{
    Console.Error.WriteLine("No valid IDs provided.");
    return 1;
}

var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
var exitCode = 0;

foreach (var id in ids)
{
    var result = UpdateField(repoRoot, id, field, value, dryRun);
    if (result != 0)
        exitCode = result;
}

if (exitCode == 0 && !dryRun)
{
    var syncScript = Path.Combine(repoRoot, "tools", "backlog-sync.cs");
    var psi = new System.Diagnostics.ProcessStartInfo("dotnet", $"run \"{syncScript}\"")
    {
        WorkingDirectory = repoRoot,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    var proc = System.Diagnostics.Process.Start(psi)!;
    var syncOut = proc.StandardOutput.ReadToEnd().Trim();
    var syncErr = proc.StandardError.ReadToEnd().Trim();
    proc.WaitForExit();
    if (!string.IsNullOrEmpty(syncOut))
        Console.WriteLine(syncOut);
    if (proc.ExitCode != 0 && !string.IsNullOrEmpty(syncErr))
        Console.Error.WriteLine(syncErr);
}

return exitCode;

List<string> ExpandIds(string[] rawIds)
{
    var result = new List<string>();
    foreach (var raw in rawIds)
    {
        if (raw.Contains(".."))
        {
            var parts = raw.Split("..", 2);
            var rangeIds = parts.Length == 2 ? ExpandRange(parts[0], parts[1]) : null;
            if (rangeIds is not null)
                result.AddRange(rangeIds);
            else
                Console.Error.WriteLine($"Invalid range: {raw}");
        }
        else
        {
            result.Add(raw);
        }
    }
    return result;
}

List<string>? ExpandRange(string startId, string endId)
{
    var startPrefix = GetPrefix(startId);
    var endPrefix = GetPrefix(endId);
    if (startPrefix is null || endPrefix is null || startPrefix != endPrefix)
        return null;

    var startNum = GetNumber(startId);
    var endNum = GetNumber(endId);
    if (startNum < 0 || endNum < 0 || endNum < startNum)
        return null;

    var ids = new List<string>();
    for (var i = startNum; i <= endNum; i++)
        ids.Add($"{startPrefix}-{i:D4}");
    return ids;
}

string? GetPrefix(string id)
{
    var dash = id.LastIndexOf('-');
    return dash > 0 ? id[..dash] : null;
}

int GetNumber(string id)
{
    var dash = id.LastIndexOf('-');
    if (dash < 0 || dash + 1 >= id.Length) return -1;
    return int.TryParse(id[(dash + 1)..], out var n) ? n : -1;
}

int UpdateField(string repoRoot, string id, string field, string value, bool dryRun)
{
    var filePath = Path.Combine(repoRoot, "backlog", "items", $"{id}.md");

    if (!File.Exists(filePath))
    {
        Console.Error.WriteLine($"{id}: file not found, skipping");
        return 1;
    }

    var text = File.ReadAllText(filePath);
    var newline = text.Contains("\r\n") ? "\r\n" : "\n";
    var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

    var firstDelimiter = lines.IndexOf("---");
    if (firstDelimiter < 0)
    {
        Console.Error.WriteLine($"{id}: no frontmatter found (missing opening ---)");
        return 1;
    }

    var secondDelimiter = lines.IndexOf("---", firstDelimiter + 1);
    if (secondDelimiter < 0)
    {
        Console.Error.WriteLine($"{id}: no frontmatter found (missing closing ---)");
        return 1;
    }

    var prefix = dryRun ? "[dry-run] " : "";

    SetOrInsert(field, value, $"{prefix}{id}: {field} ");

    if (string.Equals(field, "status", StringComparison.OrdinalIgnoreCase))
    {
        if (value is "done" or "rejected" or "wont-fix")
        {
            // Local date, not UTC: the author is in AEST, where UTC is still "yesterday" before 10am.
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            SetOrInsert("completed", today, $"{prefix}{id}: completed ");
        }
        else
        {
            SetOrInsert("completed", "null", $"{prefix}{id}: completed ");
        }
    }

    if (!dryRun)
        File.WriteAllText(filePath, string.Join(newline, lines));

    return 0;

    void SetOrInsert(string key, string newValue, string logPrefix)
    {
        var closing = lines.IndexOf("---", firstDelimiter + 1);
        for (int i = firstDelimiter + 1; i < closing; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith($"{key}:", StringComparison.OrdinalIgnoreCase))
            {
                var oldValue = trimmed[(trimmed.IndexOf(':') + 1)..].Trim().Trim('"');
                if (oldValue == newValue) return;
                Console.WriteLine($"{logPrefix}'{oldValue}' -> '{newValue}'");
                if (!dryRun)
                    lines[i] = $"{key}: {newValue}";
                return;
            }
        }

        Console.WriteLine($"{logPrefix}added as '{newValue}'");
        if (!dryRun)
            lines.Insert(closing, $"{key}: {newValue}");
    }
}

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
