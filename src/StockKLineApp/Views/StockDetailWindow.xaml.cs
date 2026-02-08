using System.Diagnostics;
using System.Windows.Threading;
using StockKLineApp.Models;
using StockKLineApp.Services;
using StockKLineApp.ViewModels;

namespace StockKLineApp.Views;

public partial class StockDetailWindow : System.Windows.Window
{
    private readonly StockDetailViewModel _viewModel;
    private readonly DispatcherTimer _throttleTimer;
    private int _pendingVisibleCount;

    public StockDetailWindow(StockListItemViewModel stock, StockDataCacheService cache)
    {
        InitializeComponent();

        _viewModel = new StockDetailViewModel(stock, cache);
        DataContext = _viewModel;

        _throttleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _throttleTimer.Tick += (_, _) =>
        {
            _throttleTimer.Stop();
            RenderVisibleRange(_pendingVisibleCount);
        };

        Loaded += (_, _) => InitializeChartAndRange();
    }

    private void InitializeChartAndRange()
    {
        CandlestickPlot.Plot.Clear();
        CandlestickPlot.Plot.Axes.DateTimeTicksBottom();
        CandlestickPlot.Plot.XLabel("日期");
        CandlestickPlot.Plot.YLabel("价格");

        var maxWindowSize = Math.Max(1, _viewModel.Bars.Count);
        RangeSlider.Maximum = maxWindowSize;
        RangeSlider.TickFrequency = Math.Max(1, maxWindowSize / 10.0);
        RangeSlider.Value = Math.Min(_viewModel.DefaultVisibleBarCount, maxWindowSize);

        _pendingVisibleCount = (int)RangeSlider.Value;
        RenderVisibleRange(_pendingVisibleCount);
    }

    private void RangeSlider_OnValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded || _viewModel.Bars.Count == 0)
        {
            return;
        }

        _pendingVisibleCount = Math.Clamp((int)Math.Round(e.NewValue), 1, _viewModel.Bars.Count);
        _throttleTimer.Stop();
        _throttleTimer.Start();
    }

    private void RenderVisibleRange(int visibleCount)
    {
        var sw = Stopwatch.StartNew();
        var startIndex = Math.Max(0, _viewModel.Bars.Count - visibleCount);
        var visibleBars = _viewModel.Bars.Skip(startIndex).Take(visibleCount).ToList();
        var ohlcs = visibleBars.Select(ToOhlc).ToList();

        var startDate = visibleBars[0].Date;
        var endDate = visibleBars[^1].Date;

        CandlestickPlot.Plot.Clear();
        CandlestickPlot.Plot.Add.Candlestick(ohlcs);
        CandlestickPlot.Plot.Axes.DateTimeTicksBottom();
        CandlestickPlot.Plot.XLabel("日期");
        CandlestickPlot.Plot.YLabel("价格");
        CandlestickPlot.Plot.Title($"{_viewModel.CodeLabel} K 线图（最近 {visibleCount} 根）");

        RangeLabel.Text = $"当前区间：{startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}";

        CandlestickPlot.Refresh();
        sw.Stop();
        RenderCostLabel.Text = $"渲染耗时：{sw.ElapsedMilliseconds} ms（节流 100ms）";
    }

    private static ScottPlot.OHLC ToOhlc(DailyBar bar)
    {
        var time = bar.Date.ToDateTime(TimeOnly.MinValue);
        return new ScottPlot.OHLC((double)bar.Open, (double)bar.High, (double)bar.Low, (double)bar.Close, time, TimeSpan.FromDays(1));
    }
}

public sealed class StockDetailViewModel
{
    public StockDetailViewModel(StockListItemViewModel stock, StockDataCacheService cache)
    {
        CodeLabel = $"代码：{stock.Code}";
        NameLabel = $"名称：{stock.Name}";
        SymbolLabel = $"Symbol：{stock.Symbol}";
        Bars = cache.GetBars(stock.CacheKey);
    }

    public const int DefaultBarCount = 200;
    public int DefaultVisibleBarCount => Math.Min(DefaultBarCount, Bars.Count);

    public string CodeLabel { get; }
    public string NameLabel { get; }
    public string SymbolLabel { get; }
    public IReadOnlyList<DailyBar> Bars { get; }
}
