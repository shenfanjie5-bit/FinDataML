using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using StockKLineApp.Models;
using StockKLineApp.Services;

namespace StockKLineApp.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly CsvStockImportService _importService = new();
    private readonly StockDataCacheService _stockCache = new();
    private readonly List<StockListItemViewModel> _allStocks = [];

    [ObservableProperty]
    private string statusMessage = "请选择 CSV 文件导入。";

    [ObservableProperty]
    private string searchKeyword = string.Empty;

    [ObservableProperty]
    private bool isImporting;

    [ObservableProperty]
    private int importProgress;

    [ObservableProperty]
    private string importProgressText = "等待导入";

    public ObservableCollection<StockListItemViewModel> Stocks { get; } = [];

    public ObservableCollection<string> Errors { get; } = [];

    public ObservableCollection<string> Logs { get; } = [];

    public event Action<StockListItemViewModel, StockDataCacheService>? NavigateToStockRequested;

    partial void OnSearchKeywordChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task OpenCsvAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsImporting = true;
        ImportProgress = 0;
        ImportProgressText = "准备导入...";
        StatusMessage = "正在导入 CSV，请稍候。";
        Logs.Clear();

        var progress = new Progress<ImportProgressInfo>(info =>
        {
            ImportProgress = info.Percent;
            ImportProgressText = info.Message;
        });

        try
        {
            var result = await Task.Run(() => _importService.Import(dialog.FileName, progress));

            _stockCache.ReplaceWith(result.Stocks);
            _allStocks.Clear();
            _allStocks.AddRange(result.Stocks.Select(s => new StockListItemViewModel
            {
                Code = s.Code,
                Name = s.Name,
                Symbol = s.Symbol,
                CacheKey = StockDataCacheService.BuildKey(s.Code, s.Name),
                BarCount = s.Bars.Count
            }));

            Errors.Clear();
            foreach (var error in result.Errors)
            {
                var line = error.ToString();
                Errors.Add(line);
                Logs.Add($"[WARN] {line}");
            }

            ApplyFilter();
            ImportProgress = 100;
            ImportProgressText = "导入完成";
            StatusMessage = $"导入完成：股票 {result.Stocks.Count} 只，错误 {result.Errors.Count} 条，编码 {result.EncodingUsed}。";
            Logs.Add($"[INFO] CSV 导入完成，股票缓存已更新。编码: {result.EncodingUsed}。");
        }
        catch (Exception ex)
        {
            StatusMessage = $"导入失败：{ex.Message}";
            Logs.Add($"[ERROR] {ex}");
        }
        finally
        {
            IsImporting = false;
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

        NavigateToStockRequested?.Invoke(stock, _stockCache);
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
