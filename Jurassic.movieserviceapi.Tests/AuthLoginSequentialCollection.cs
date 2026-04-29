using Xunit;

namespace Jurassic.movieserviceapi.Tests;

/// <summary>Serializes auth login tests that share a single host fixture and a mutable auth repository mock.</summary>
[CollectionDefinition("AuthLoginSequential", DisableParallelization = true)]
public sealed class AuthLoginSequentialCollection;
