using StockKLineApp.Models;
using StockKLineApp.ViewModels;

namespace StockKLineApp.Views;

public partial class StockDetailWindow : System.Windows.Window
{
    private readonly StockDetailViewModel _viewModel;
    private readonly List<ScottPlot.OHLC> _ohlcs;

    public StockDetailWindow(StockListItemViewModel stock)
    {
        InitializeComponent();

        _viewModel = new StockDetailViewModel(stock);
        DataContext = _viewModel;
        _ohlcs = _viewModel.Bars.Select(ToOhlc).ToList();

        Loaded += (_, _) => InitializeChartAndRange();
    }

    private void InitializeChartAndRange()
    {
        CandlestickPlot.Plot.Clear();
        CandlestickPlot.Plot.Add.Candlestick(_ohlcs);
        CandlestickPlot.Plot.Axes.DateTimeTicksBottom();
        CandlestickPlot.Plot.XLabel("日期");
        CandlestickPlot.Plot.YLabel("价格");

        var maxWindowSize = Math.Max(1, _viewModel.Bars.Count);
        RangeSlider.Maximum = maxWindowSize;
        RangeSlider.TickFrequency = Math.Max(1, maxWindowSize / 10.0);
        RangeSlider.Value = Math.Min(_viewModel.DefaultVisibleBarCount, maxWindowSize);

        UpdateVisibleRange((int)RangeSlider.Value);
    }

    private void RangeSlider_OnValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded || _viewModel.Bars.Count == 0)
        {
            return;
        }

        var visibleCount = Math.Clamp((int)Math.Round(e.NewValue), 1, _viewModel.Bars.Count);
        UpdateVisibleRange(visibleCount);
    }

    private void UpdateVisibleRange(int visibleCount)
    {
        var startIndex = Math.Max(0, _viewModel.Bars.Count - visibleCount);
        var startDate = _viewModel.Bars[startIndex].Date;
        var endDate = _viewModel.Bars[^1].Date;

        var minX = startDate.ToDateTime(TimeOnly.MinValue).ToOADate();
        var maxX = endDate.ToDateTime(TimeOnly.MinValue).AddDays(1).ToOADate();

        CandlestickPlot.Plot.Axes.SetLimitsX(minX, maxX);
        CandlestickPlot.Plot.Title($"{_viewModel.CodeLabel} K 线图（最近 {visibleCount} 根）");
        RangeLabel.Text = $"当前区间：{startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}";
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
        Bars = stock.Bars.ToList();
    }

    public const int DefaultBarCount = 200;
    public int DefaultVisibleBarCount => Math.Min(DefaultBarCount, Bars.Count);

    public string CodeLabel { get; }
    public string NameLabel { get; }
    public string SymbolLabel { get; }
    public IReadOnlyList<DailyBar> Bars { get; }
}
