using System.Text.RegularExpressions;

namespace Jurassic.movieserviceapi.Utilities;

/// <summary>Seat labels displayed as row + seat number, e.g. <c>A12</c> or <c>A1</c>.</summary>
public static partial class SeatLabelNormalizer
{
    public static string Normalize(string raw)
    {
        var trimmed = raw.Trim();
        var m = SeatLabelRegex().Match(trimmed);
        return m.Success
            ? m.Groups[1].Value.ToUpperInvariant() + m.Groups[2].Value
            : trimmed.ToUpperInvariant();
    }

    [GeneratedRegex(@"^\s*([A-Za-z]+)\s*(\d+)\s*$", RegexOptions.Compiled)]
    private static partial Regex SeatLabelRegex();
}
