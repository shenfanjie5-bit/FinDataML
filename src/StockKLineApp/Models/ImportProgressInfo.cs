namespace StockKLineApp.Models;

public sealed class ImportProgressInfo
{
    public required int Percent { get; init; }
    public required string Message { get; init; }
}
