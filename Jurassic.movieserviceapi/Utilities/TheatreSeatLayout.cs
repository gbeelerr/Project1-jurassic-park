namespace Jurassic.movieserviceapi.Utilities;

/// <summary>Canonical 6×8 floor plan used by API and Blazor seat map.</summary>
public static class TheatreSeatLayout
{
    public static readonly string[] Rows = ["A", "B", "C", "D", "E", "F"];

    public static IReadOnlyList<string> AllSeatLabels()
    {
        var list = new List<string>(48);
        foreach (var row in Rows)
        {
            for (var n = 1; n <= 8; n++)
            {
                list.Add($"{row}{n}");
            }
        }

        return list;
    }
}
