namespace StockKLineApp.Models;

public sealed class StockSeries
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Symbol { get; init; }

    public List<DailyBar> Bars { get; } = new();
}
