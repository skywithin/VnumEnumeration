// Validates every backlog item file: required frontmatter, known values, ID/filename match,
// completed-date consistency, stale blockers and suspicious closes.
// Usage: dotnet run tools/backlog-lint.cs [-- --fix] [-- --scope open|done|all]
//
// Checks:
//   - Required fields present (id, title, type, area, priority, size, status)
//   - ID matches filename; no duplicate IDs; title not empty
//   - Field values in known sets (type, status, priority, size, area, breaking)
//   - Completed date set on terminal statuses (done, rejected, wont-fix) and absent otherwise
//   - Open items whose blocked_by points at a closed item (stale block)
//   - Done items with every acceptance checkbox unticked (suspicious close)
//
// --fix    Auto-correct area casing, missing completed date (inferred from git log), stale
//          completed date on open items, and stale blocked_by. Rebuilds the index afterwards.
// --scope  Report only on open (non-terminal), done (terminal) or all (default) items.
// Exit code 1 when errors remain in scope, else 0.

var fix = args.Any(a => a == "--fix");
var scopeIdx = Array.FindIndex(args, a => a == "--scope");
var scope = scopeIdx >= 0 && scopeIdx + 1 < args.Length ? args[scopeIdx + 1] : "all";

var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
var itemsDir = Path.Combine(repoRoot, "backlog", "items");

if (!Directory.Exists(itemsDir))
{
    Console.Error.WriteLine($"Items directory not found: {itemsDir}");
    return 1;
}

var validTypes = new HashSet<string> { "enhancement", "feature", "tech-debt", "test-gap", "bug", "investigation", "chore" };
var validStatuses = new HashSet<string> { "open", "in-progress", "done", "rejected", "wont-fix" };
var validPriorities = new HashSet<string> { "critical", "high", "medium", "low" };
var validSizes = new HashSet<string> { "S", "M", "L", "XL" };
var validBreaking = new HashSet<string> { "true", "false", "null" };
var canonicalAreas = new[] { "core", "serialization", "tests", "packaging", "docs" }
    .ToDictionary(a => a, a => a, StringComparer.OrdinalIgnoreCase);
var terminalStatuses = new HashSet<string> { "done", "rejected", "wont-fix" };
var requiredFields = new[] { "id", "title", "type", "area", "priority", "size", "status" };

var files = Directory.GetFiles(itemsDir, "*.md").OrderBy(f => f, StringComparer.Ordinal).ToArray();
var errors = new List<string>();
var warnings = new List<string>();
var fixes = new List<string>();
var seenIds = new Dictionary<string, string>();
var statusByFile = new Dictionary<string, string>();

foreach (var filePath in files)
{
    var fileName = Path.GetFileNameWithoutExtension(filePath);
    var text = File.ReadAllText(filePath);
    var newline = text.Contains("\r\n") ? "\r\n" : "\n";
    var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

    if (lines.Count < 2 || lines[0].Trim() != "---")
    {
        errors.Add($"{fileName}: missing frontmatter (no opening ---)");
        continue;
    }

    var secondDelimiter = -1;
    for (int i = 1; i < lines.Count; i++)
    {
        if (lines[i].Trim() == "---") { secondDelimiter = i; break; }
    }

    if (secondDelimiter < 0)
    {
        errors.Add($"{fileName}: missing frontmatter (no closing ---)");
        continue;
    }

    var fm = new Dictionary<string, (string Value, int LineIndex)>(StringComparer.OrdinalIgnoreCase);
    for (int i = 1; i < secondDelimiter; i++)
    {
        var line = lines[i].Trim();
        var colon = line.IndexOf(':');
        if (colon <= 0) continue;
        fm[line[..colon].Trim()] = (Unquote(line[(colon + 1)..]), i);
    }

    var status = fm.TryGetValue("status", out var statusEntry) ? statusEntry.Value : "";
    var isTerminal = terminalStatuses.Contains(status);
    statusByFile[fileName] = status;

    foreach (var field in requiredFields)
    {
        if (!fm.TryGetValue(field, out var entry) || string.IsNullOrWhiteSpace(entry.Value) || entry.Value == "null")
        {
            if (isTerminal)
                warnings.Add($"{fileName}: missing field '{field}' (closed item)");
            else
                errors.Add($"{fileName}: missing required field '{field}'");
        }
    }

    if (!fm.TryGetValue("id", out var idEntry))
        continue;

    var id = idEntry.Value;

    if (!string.Equals(id, fileName, StringComparison.Ordinal))
        errors.Add($"{fileName}: id '{id}' does not match filename");

    if (seenIds.TryGetValue(id, out var existingFile))
        errors.Add($"{fileName}: duplicate id '{id}' (also in {existingFile})");
    else
        seenIds[id] = fileName;

    var expectedPrefix = fm.TryGetValue("type", out var t0) && t0.Value == "bug" ? "BUG-" : "BL-";
    if (!id.StartsWith(expectedPrefix, StringComparison.Ordinal))
        warnings.Add($"{fileName}: id prefix does not match type (expected {expectedPrefix}XXXX)");

    if (fm.TryGetValue("type", out var typeEntry) && !validTypes.Contains(typeEntry.Value))
        warnings.Add($"{fileName}: unknown type '{typeEntry.Value}'");

    if (!string.IsNullOrEmpty(status) && !validStatuses.Contains(status))
        warnings.Add($"{fileName}: unknown status '{status}'");

    if (fm.TryGetValue("priority", out var prioEntry) && !validPriorities.Contains(prioEntry.Value))
        warnings.Add($"{fileName}: unknown priority '{prioEntry.Value}'");

    if (fm.TryGetValue("size", out var sizeEntry) && sizeEntry.Value is not ("" or "null") && !validSizes.Contains(sizeEntry.Value))
        warnings.Add($"{fileName}: unknown size '{sizeEntry.Value}'");

    if (fm.TryGetValue("breaking", out var breakingEntry) && !validBreaking.Contains(breakingEntry.Value))
        warnings.Add($"{fileName}: breaking must be true or false, found '{breakingEntry.Value}'");

    if (fm.TryGetValue("area", out var areaEntry))
    {
        var area = areaEntry.Value;
        if (canonicalAreas.TryGetValue(area, out var canonical))
        {
            if (area != canonical)
            {
                if (fix)
                {
                    lines[areaEntry.LineIndex] = $"area: {canonical}";
                    fixes.Add($"{fileName}: area '{area}' -> '{canonical}'");
                }
                else
                {
                    warnings.Add($"{fileName}: area '{area}' should be '{canonical}' (use --fix)");
                }
            }
        }
        else
        {
            warnings.Add($"{fileName}: unknown area '{area}' (known: {string.Join(", ", canonicalAreas.Values)})");
        }
    }

    var hasCompleted = fm.TryGetValue("completed", out var completedEntry)
        && completedEntry.Value is not ("" or "null");

    if (isTerminal && !hasCompleted)
    {
        if (fix)
        {
            var inferred = InferCompletedDate(repoRoot, filePath, status);
            if (inferred is not null)
            {
                if (fm.ContainsKey("completed"))
                    lines[fm["completed"].LineIndex] = $"completed: {inferred}";
                else
                {
                    lines.Insert(secondDelimiter, $"completed: {inferred}");
                    secondDelimiter++;
                }
                fixes.Add($"{fileName}: completed inferred from git log as '{inferred}'");
            }
            else
            {
                warnings.Add($"{fileName}: status is '{status}' but completed is missing; no 'status: {status}' commit found in git log (set it manually)");
            }
        }
        else
        {
            warnings.Add($"{fileName}: status is '{status}' but completed is missing (use --fix)");
        }
    }

    if (!isTerminal && hasCompleted)
    {
        if (fix)
        {
            lines[completedEntry.LineIndex] = "completed: null";
            fixes.Add($"{fileName}: completed cleared (status is '{status}')");
        }
        else
        {
            warnings.Add($"{fileName}: status is '{status}' but completed is '{completedEntry.Value}' (use --fix)");
        }
    }

    // Open item blocked by an item that has since closed.
    var hasBlockedBy = fm.TryGetValue("blocked_by", out var blockedByEntry)
        && blockedByEntry.Value is not ("" or "null");

    if (hasBlockedBy && !isTerminal)
    {
        var match = System.Text.RegularExpressions.Regex.Match(blockedByEntry.Value, @"(BL-\d{4}|BUG-\d{4})");
        if (match.Success)
        {
            var refFile = Path.Combine(itemsDir, $"{match.Value}.md");
            if (File.Exists(refFile))
            {
                var refStatus = File.ReadLines(refFile)
                    .Select(l => l.Trim())
                    .FirstOrDefault(l => l.StartsWith("status:"))?
                    .Split(':', 2)[1].Trim().Trim('"');

                if (refStatus is not null && terminalStatuses.Contains(refStatus))
                {
                    if (fix)
                    {
                        lines[blockedByEntry.LineIndex] = "blocked_by: null";
                        fixes.Add($"{fileName}: blocked_by cleared ({match.Value} is '{refStatus}')");
                    }
                    else
                    {
                        warnings.Add($"{fileName}: blocked_by references {match.Value} which is '{refStatus}' (stale block, use --fix)");
                    }
                }
            }
            else
            {
                warnings.Add($"{fileName}: blocked_by references {match.Value}, which does not exist");
            }
        }
    }

    // Done items with every checkbox unticked. Rejected / wont-fix are closed without the work.
    if (status == "done")
    {
        var body = lines.Skip(secondDelimiter + 1).Select(l => l.TrimStart()).ToList();
        var hasChecked = body.Any(l => l.StartsWith("- [x]") || l.StartsWith("- [X]"));
        var hasUnchecked = body.Any(l => l.StartsWith("- [ ]"));
        if (hasUnchecked && !hasChecked)
            warnings.Add($"{fileName}: status is 'done' but every acceptance checkbox is unticked; verify completion");
    }

    if (fix && fixes.Any(f => f.StartsWith(fileName + ":")))
        File.WriteAllText(filePath, string.Join(newline, lines));
}

bool IsInScope(string msg)
{
    if (scope == "all") return true;
    var itemId = msg.Split(':')[0].Trim();
    var st = statusByFile.GetValueOrDefault(itemId, "");
    var closed = terminalStatuses.Contains(st);
    return scope == "open" ? !closed : scope == "done" ? closed : true;
}

var scopedErrors = errors.Where(IsInScope).ToList();
var scopedWarnings = warnings.Where(IsInScope).ToList();
var scopeLabel = scope != "all" ? $" (scope: {scope})" : "";

if (fixes.Count > 0)
{
    Console.WriteLine($"Fixed ({fixes.Count}):");
    foreach (var f in fixes) Console.WriteLine($"  {f}");
    Console.WriteLine();
}

if (scopedErrors.Count > 0)
{
    Console.WriteLine($"Errors ({scopedErrors.Count}){scopeLabel}:");
    foreach (var e in scopedErrors) Console.WriteLine($"  {e}");
    Console.WriteLine();
}

if (scopedWarnings.Count > 0)
{
    Console.WriteLine($"Warnings ({scopedWarnings.Count}){scopeLabel}:");
    foreach (var w in scopedWarnings) Console.WriteLine($"  {w}");
    Console.WriteLine();
}

if (scopedErrors.Count == 0 && scopedWarnings.Count == 0 && fixes.Count == 0)
    Console.WriteLine($"All {files.Length} items OK{scopeLabel}.");
else
    Console.WriteLine($"Scanned {files.Length} items{scopeLabel}: {scopedErrors.Count} errors, {scopedWarnings.Count} warnings, {fixes.Count} fixes applied.");

if (fix && fixes.Count > 0)
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
    proc.WaitForExit();
    if (!string.IsNullOrEmpty(syncOut))
        Console.WriteLine(syncOut);
}

return scopedErrors.Count > 0 ? 1 : 0;

// Helpers

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

// Finds the commit that introduced "status: <status>" into the item file and returns its date.
// Returns null when git finds nothing, so the caller warns instead of inventing today's date.
string? InferCompletedDate(string repoRoot, string filePath, string status)
{
    try
    {
        var relPath = Path.GetRelativePath(repoRoot, filePath).Replace('\\', '/');
        var psi = new System.Diagnostics.ProcessStartInfo("git",
            $"log -1 --format=%cs -S\"status: {status}\" -- \"{relPath}\"")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc is null) return null;
        var output = proc.StandardOutput.ReadToEnd().Trim();
        if (!proc.WaitForExit(5000))
        {
            try { proc.Kill(); } catch { }
            return null;
        }
        if (DateTime.TryParseExact(output, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _))
            return output;
    }
    catch
    {
        // git unavailable: fall through
    }
    return null;
}
