/// <summary>
/// Conventional Commits linter
/// https://www.conventionalcommits.org/en/v1.0.0/
/// </summary>

using System.Text.RegularExpressions;

private var pattern = @"^(?=.{1,90}$)(?:feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(?:\([^)]+\))?!?(?::)\s.{3,}(?:#\d+)*(?<!\s)$";
private var msg = File.ReadAllLines(Args[0])[0];

// Bypass merge commits, squash merges, and reverts
if (Regex.IsMatch(msg, @"^(Merge |Squash |Revert "")"))
    return 0;

if (Regex.IsMatch(msg, pattern))
    return 0;

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("Invalid commit message format.");
Console.ResetColor();
Console.WriteLine();
Console.WriteLine("Expected: <type>[optional scope][!]: <description>");
Console.WriteLine();
Console.WriteLine("  Valid types: feat fix docs style refactor perf test build ci chore revert");
Console.WriteLine();
Console.WriteLine("  Examples:");
Console.WriteLine("    feat: add backup encryption");
Console.WriteLine("    fix(model): handle null file path");
Console.WriteLine("    feat!: breaking API change");
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine("https://www.conventionalcommits.org/en/v1.0.0/");
Console.ResetColor();

return 1;
