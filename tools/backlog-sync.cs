// Regenerates backlog/BACKLOG.md from backlog/items/*.md frontmatter.
// Usage: dotnet run tools/backlog-sync.cs [-- --dry-run]
//
// Keeps the static header (everything before "## Next major version" or "## Open") and
// rebuilds the "Next major version", "Open" and "Done (recent)" sections from the items.
// Writes LF line endings.
//
// --dry-run  Report whether the file would change, without writing it.

var dryRun = args.Any(a => a == "--dry-run");

var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
var itemsDir = Path.Combine(repoRoot, "backlog", "items");
var backlogFile = Path.Combine(repoRoot, "backlog", "BACKLOG.md");

if (!Directory.Exists(itemsDir))
{
    Console.Error.WriteLine($"Items directory not found: {itemsDir}");
    return 1;
}

// Section mapping (type -> heading). Order controls display order under "## Open".
var sectionMap = new (string Heading, Func<BacklogItem, bool> Match)[]
{
    ("Bugs",                    i => i.Type == "bug"),
    ("Features & Enhancements", i => i.Type is "feature" or "enhancement"),
    ("Test Coverage",           i => i.Type == "test-gap"),
    ("Tech Debt",               i => i.Type is "tech-debt" or "chore"),
    ("Investigations",          i => i.Type == "investigation"),
};

var items = Directory.GetFiles(itemsDir, "*.md")
    .Select(ParseItem)
    .OfType<BacklogItem>()
    .ToList();

var open = items
    .Where(i => i.Status is "open" or "in-progress")
    .OrderBy(i => PriorityRank(i.Priority))
    .ThenBy(i => i.Id, StringComparer.Ordinal)
    .ToList();

var done = items
    .Where(i => i.Status is "done" or "rejected" or "wont-fix")
    .OrderByDescending(i => i.Completed ?? "0000-00-00")
    .ThenByDescending(i => i.Id, StringComparer.Ordinal)
    .ToList();

// Static header: everything before the first generated section.
var headerLines = new List<string>();
if (File.Exists(backlogFile))
{
    foreach (var raw in File.ReadAllLines(backlogFile))
    {
        var t = raw.TrimStart();
        if (t.StartsWith("## Open") || t.StartsWith("## Next major version"))
            break;
        headerLines.Add(raw);
    }
}
else
{
    headerLines.Add("# Backlog");
    headerLines.Add("");
}

var sb = new System.Text.StringBuilder();
foreach (var h in headerLines)
    Line(h);

var breaking = open.Where(i => i.Breaking).ToList();
if (breaking.Count > 0)
{
    Line("## Next major version");
    Line();
    Line("Open items with `breaking: true`: they change the public API or documented behaviour, so ship them together in one major version. They are listed again under Open.");
    Line();
    WriteTable(breaking);
}

Line("## Open");
Line();

foreach (var (heading, match) in sectionMap)
{
    var sectionItems = open.Where(match).ToList();
    if (sectionItems.Count == 0) continue;
    Line($"### {heading}");
    Line();
    WriteTable(sectionItems);
}

var other = open.Where(i => !sectionMap.Any(s => s.Match(i))).ToList();
if (other.Count > 0)
{
    Line("### Other");
    Line();
    WriteTable(other);
}

if (open.Count == 0)
{
    Line("No open items.");
    Line();
}

Line("## Done (recent)");
Line();
Line("Older completed items stay in `backlog/items/`; this table shows the most recent completions.");
Line();
if (done.Count > 0)
    WriteTable(done.Take(30));
else
{
    Line("Nothing completed yet.");
    Line();
}

var newContent = sb.ToString();
var oldContent = File.Exists(backlogFile) ? File.ReadAllText(backlogFile) : null;
var summary = $"{open.Count} open ({breaking.Count} breaking), {done.Count} done ({Math.Min(done.Count, 30)} shown)";

if (dryRun)
{
    Console.WriteLine(oldContent == newContent ? "No changes." : $"[dry-run] BACKLOG.md would be updated: {summary}.");
    return 0;
}

if (oldContent == newContent)
{
    Console.WriteLine($"BACKLOG.md unchanged: {summary}.");
    return 0;
}

File.WriteAllText(backlogFile, newContent);
Console.WriteLine($"BACKLOG.md regenerated: {summary}.");
return 0;

// Helpers

void Line(string s = "") => sb.Append(s).Append('\n');

void WriteTable(IEnumerable<BacklogItem> rows)
{
    var list = rows.ToList();
    var blocked = list.Any(i => i.BlockedBy is not null);

    Line(blocked
        ? "| ID | Title | Type | Area | Priority | Size | Blocked |"
        : "| ID | Title | Type | Area | Priority | Size |");
    Line(blocked
        ? "|----|-------|------|------|----------|------|---------|"
        : "|----|-------|------|------|----------|------|");

    foreach (var i in list)
    {
        var row = $"| {i.Id} | {Cell(i.Title)} | {i.Type} | {i.Area} | {i.Priority} | {i.Size} |";
        if (blocked) row += $" {Cell(i.BlockedBy ?? "")} |";
        Line(row);
    }
    Line();
}

string Cell(string s) => s.Replace("|", "\\|");

BacklogItem? ParseItem(string filePath)
{
    var lines = File.ReadAllLines(filePath);
    if (lines.Length < 2 || lines[0].Trim() != "---") return null;

    var fm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (int i = 1; i < lines.Length; i++)
    {
        var line = lines[i].Trim();
        if (line == "---") break;

        var colon = line.IndexOf(':');
        if (colon <= 0) continue;

        fm[line[..colon].Trim()] = Unquote(line[(colon + 1)..]);
    }

    if (!fm.TryGetValue("id", out var id)) return null;
    if (!fm.TryGetValue("status", out var status)) return null;

    return new BacklogItem
    {
        Id = id,
        Title = fm.GetValueOrDefault("title", "Untitled"),
        Type = fm.GetValueOrDefault("type", "unknown"),
        Area = fm.GetValueOrDefault("area", "unknown"),
        Priority = fm.GetValueOrDefault("priority", "medium"),
        Size = fm.GetValueOrDefault("size", "-"),
        Status = status,
        Completed = Optional(fm, "completed"),
        Breaking = fm.GetValueOrDefault("breaking", "false") == "true",
        BlockedBy = Optional(fm, "blocked_by"),
    };
}

string? Optional(Dictionary<string, string> fm, string key) =>
    fm.TryGetValue(key, out var v) && v is not "null" and not "" ? v : null;

string Unquote(string v)
{
    v = v.Trim();
    if (v.Length >= 2 && v[0] == '"' && v[^1] == '"')
        return v[1..^1].Replace("\\\"", "\"");
    return v;
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

int PriorityRank(string priority) => priority switch
{
    "critical" => 0,
    "high" => 1,
    "medium" => 2,
    "low" => 3,
    _ => 4,
};

class BacklogItem
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Type { get; init; } = "";
    public string Area { get; init; } = "";
    public string Priority { get; init; } = "";
    public string Size { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Completed { get; init; }
    public bool Breaking { get; init; }
    public string? BlockedBy { get; init; }
}
