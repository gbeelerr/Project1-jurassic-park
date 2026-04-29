using FluentAssertions;

namespace Jurassic.movieserviceapi.Tests;

public class DatabaseBootstrapTests
{
    [Fact]
    public void BootstrapScript_ShouldSeedShowtimesRelativeToCurrentDate()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jurassic.sln")))
        {
            dir = dir.Parent;
        }

        var solutionRoot = dir?.FullName
                           ?? throw new InvalidOperationException("Could not locate Jurassic.sln from test output directory.");
        var scriptPath = Path.Combine(solutionRoot, "Jurassic.movieserviceapi", "Data", "bootstrap.sql");

        File.Exists(scriptPath).Should().BeTrue("the movieservice bootstrap script should be present in source control");

        var script = File.ReadAllText(scriptPath);

        script.Should().Contain("CURRENT_DATE");
        script.Should().NotContain("2026-04-05");
        script.Should().NotContain("2026-04-06");
    }
}
