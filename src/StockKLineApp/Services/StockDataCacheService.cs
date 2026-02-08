using StockKLineApp.Models;

namespace StockKLineApp.Services;

public sealed class StockDataCacheService
{
    private readonly Dictionary<string, IReadOnlyList<DailyBar>> _barsByStock = new(StringComparer.OrdinalIgnoreCase);

    public void ReplaceWith(IReadOnlyList<StockSeries> stocks)
    {
        _barsByStock.Clear();
        foreach (var stock in stocks)
        {
            _barsByStock[BuildKey(stock.Code, stock.Name)] = stock.Bars.ToList();
        }
    }

    public IReadOnlyList<DailyBar> GetBars(string cacheKey)
    {
        if (_barsByStock.TryGetValue(cacheKey, out var bars))
        {
            return bars;
        }

        throw new KeyNotFoundException($"未找到股票缓存数据：{cacheKey}");
    }

    public static string BuildKey(string code, string name) => $"{code}|{name}";
}
