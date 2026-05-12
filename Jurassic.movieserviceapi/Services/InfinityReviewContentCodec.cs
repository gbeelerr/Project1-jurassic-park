using System.Text.RegularExpressions;

namespace Jurassic.movieserviceapi.Services;

public static partial class InfinityReviewContentCodec
{
    private static readonly Regex StarPrefixRegex = BuildRegex();

    public static string Encode(int stars, string content)
    {
        return content.Trim();
    }

    public static (int stars, string content) Decode(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (0, "");
        }

        var match = StarPrefixRegex.Match(content);
        if (match.Success)
        {
            var stars = int.TryParse(match.Groups["stars"].Value, out var value) ? Math.Clamp(value, 1, 5) : 0;
            var stripped = content[match.Length..].TrimStart();
            return (stars, stripped);
        }

        return (0, content.Trim());
    }

    [GeneratedRegex(@"^\s*\[\[stars:(?<stars>[1-5])\]\]\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BuildRegex();
}
