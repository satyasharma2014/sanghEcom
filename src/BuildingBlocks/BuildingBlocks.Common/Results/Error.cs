namespace BuildingBlocks.Common.Results;

/// <summary>
/// Business error. Code machine ke liye (jaise "STOCK_LOW"), Message insaan ke liye.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
