using System.Text.Json.Serialization;

namespace Jurassic.movieserviceapi.Models;

public sealed class ShowtimeSeatItem
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("available")]
    public bool Available { get; set; }
}
