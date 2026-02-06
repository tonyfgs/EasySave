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
}
