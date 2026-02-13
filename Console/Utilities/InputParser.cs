namespace EasySave.Utilities;

public class InputParser
{
    public List<int> ParseJobRange(string input)
    {
        var parts = input.Split('-');
        int start = int.Parse(parts[0].Trim());
        int end = int.Parse(parts[1].Trim());
        int count = end - start + 1;
        if (count <= 0)
            return new List<int>();
        return Enumerable.Range(start, count).ToList();
    }

    public List<int> ParseJobList(string input)
    {
        return input.Split(';')
            .Select(s => int.Parse(s.Trim()))
            .ToList();
    }

    public List<int> ParseInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new FormatException(
                $"Invalid input: '{input}'. Expected a job ID (e.g., 1), a range (e.g., 1-3), or a list (e.g., 1;3).");

        try
        {
            if (input.Contains(';'))
                return ParseJobList(input);
            if (input.Contains('-'))
                return ParseJobRange(input);
            return new List<int> { int.Parse(input.Trim()) };
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new FormatException(
                $"Invalid input: '{input}'. Expected a job ID (e.g., 1), a range (e.g., 1-3), or a list (e.g., 1;3).");
        }
    }
}
