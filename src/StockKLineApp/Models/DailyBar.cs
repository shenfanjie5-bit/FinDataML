namespace StockKLineApp.Models;

public sealed class DailyBar
{
    public DateOnly Date { get; init; }
    public decimal Open { get; init; }
    public decimal Close { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal? TurnoverWan { get; init; }
}
