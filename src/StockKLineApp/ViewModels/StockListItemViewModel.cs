using StockKLineApp.Models;

namespace StockKLineApp.ViewModels;

public sealed class StockListItemViewModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Symbol { get; init; }
    public required IReadOnlyList<DailyBar> Bars { get; init; }

    public string Display => $"{Code} - {Name}";
}
