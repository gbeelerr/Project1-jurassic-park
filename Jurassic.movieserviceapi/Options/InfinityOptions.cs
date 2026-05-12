namespace Jurassic.movieserviceapi.Options;

public sealed class InfinityOptions
{
    public const string SectionName = "Infinity";

    public string WebApiBaseUrl { get; set; } = "http://localhost:8080";
    public string WebAppBaseUrl { get; set; } = "http://localhost:8082";
    public string JurassicParkId { get; set; } = "park_florida_usa";
    public string MovieCategoryName { get; set; } = "Movie";
}
