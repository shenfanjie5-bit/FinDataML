using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StockKLineApp.Services;

namespace StockKLineApp.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly CsvStockImportService _importService = new();
    private readonly List<StockListItemViewModel> _allStocks = [];

    [ObservableProperty]
    private string statusMessage = "请选择 CSV 文件导入。";

    [ObservableProperty]
    private string searchKeyword = string.Empty;

    public MainViewModel()
    {
    }

    public ObservableCollection<StockListItemViewModel> Stocks { get; } = [];

    public ObservableCollection<string> Errors { get; } = [];

    public event Action<StockListItemViewModel>? NavigateToStockRequested;

    partial void OnSearchKeywordChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private void OpenCsv()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var result = _importService.Import(dialog.FileName);

            _allStocks.Clear();
            _allStocks.AddRange(result.Stocks.Select(s => new StockListItemViewModel
            {
                Code = s.Code,
                Name = s.Name,
                Symbol = s.Symbol,
                Bars = s.Bars
            }));

            Errors.Clear();
            foreach (var error in result.Errors)
            {
                Errors.Add(error.ToString());
            }

            ApplyFilter();
            StatusMessage = $"导入完成：股票 {result.Stocks.Count} 只，错误 {result.Errors.Count} 条，编码 {result.EncodingUsed}。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"导入失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void SubmitSearch()
    {
        var matches = GetMatches(SearchKeyword);
        if (matches.Count == 0)
        {
            StatusMessage = "未找到匹配股票，请尝试其他关键字。";
            return;
        }

        if (matches.Count == 1)
        {
            OpenStock(matches[0]);
            return;
        }

        StatusMessage = $"找到 {matches.Count} 条匹配，请从列表中点击目标股票。";
    }

    [RelayCommand]
    private void OpenStock(StockListItemViewModel? stock)
    {
        if (stock is null)
        {
            return;
        }

        NavigateToStockRequested?.Invoke(stock);
    }

    private void ApplyFilter()
    {
        var filtered = GetMatches(SearchKeyword);

        Stocks.Clear();
        foreach (var item in filtered)
        {
            Stocks.Add(item);
        }
    }

    private List<StockListItemViewModel> GetMatches(string? keyword)
    {
        var normalized = keyword?.Trim() ?? string.Empty;

        return string.IsNullOrWhiteSpace(normalized)
            ? [.. _allStocks]
            : _allStocks.Where(s =>
                s.Code.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
