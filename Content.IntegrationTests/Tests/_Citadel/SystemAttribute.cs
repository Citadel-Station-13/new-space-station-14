namespace Content.IntegrationTests.Tests._Citadel;

/// <summary>
///     Marks a field on a CitadelGameTest inheritor as needing to be populated with a system from the given side.
/// </summary>
public sealed class SystemAttribute(Side side) : Attribute
{
    public Side Side { get; } = side;
}

/// <summary>
///     Marks a field on a CitadelGameTest inheritor as needing to be populated with an IoC dependency from the given side.
/// </summary>
public sealed class SidedDependencyAttribute(Side side) : Attribute
{
    public Side Side { get; } = side;
}

public enum Side
{
    Client,
    Server
}
