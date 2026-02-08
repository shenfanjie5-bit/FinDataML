using StockKLineApp.Models;
using StockKLineApp.ViewModels;

namespace StockKLineApp.Views;

public partial class StockDetailWindow : System.Windows.Window
{
    private readonly StockDetailViewModel _viewModel;

    public StockDetailWindow(StockListItemViewModel stock)
    {
        InitializeComponent();

        _viewModel = new StockDetailViewModel(stock);
        DataContext = _viewModel;

        Loaded += (_, _) => RenderCandlesticks();
    }

    private void RenderCandlesticks()
    {
        var ohlcs = _viewModel.RecentBars.Select(ToOhlc);

        CandlestickPlot.Plot.Clear();
        CandlestickPlot.Plot.Add.Candlestick(ohlcs);
        CandlestickPlot.Plot.Axes.DateTimeTicksBottom();
        CandlestickPlot.Plot.XLabel("日期");
        CandlestickPlot.Plot.YLabel("价格");
        CandlestickPlot.Plot.Title($"{_viewModel.CodeLabel} K 线图（最近 {_viewModel.RecentBars.Count} 根）");
        CandlestickPlot.Refresh();
    }

    private static ScottPlot.OHLC ToOhlc(DailyBar bar)
    {
        var time = bar.Date.ToDateTime(TimeOnly.MinValue);
        return new ScottPlot.OHLC((double)bar.Open, (double)bar.High, (double)bar.Low, (double)bar.Close, time, TimeSpan.FromDays(1));
    }
}

public sealed class StockDetailViewModel
{
    public StockDetailViewModel(StockListItemViewModel stock)
    {
        CodeLabel = $"代码：{stock.Code}";
        NameLabel = $"名称：{stock.Name}";
        SymbolLabel = $"Symbol：{stock.Symbol}";
        RecentBars = stock.Bars.TakeLast(DefaultBarCount).ToList();
    }

    private const int DefaultBarCount = 200;

    public string CodeLabel { get; }
    public string NameLabel { get; }
    public string SymbolLabel { get; }
    public IReadOnlyList<DailyBar> RecentBars { get; }
}
