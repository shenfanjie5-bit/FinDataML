namespace StockKLineApp.Models;

public class StockSummary
{
    public required string Symbol { get; init; }

    public required string Name { get; init; }

    public override string ToString() => $"{Symbol} - {Name}";
}
