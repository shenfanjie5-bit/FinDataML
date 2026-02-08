using System.Collections.ObjectModel;
using Microsoft.Win32;
using StockKLineApp.Services;

namespace StockKLineApp.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly CsvStockImportService _importService = new();
    private readonly List<StockListItemViewModel> _allStocks = [];

    private string _statusMessage = "请选择 CSV 文件导入。";
    private string _searchKeyword = string.Empty;

    public MainViewModel()
    {
        OpenCsvCommand = new RelayCommand(OpenCsv);
    }

    public ObservableCollection<StockListItemViewModel> Stocks { get; } = [];

    public ObservableCollection<string> Errors { get; } = [];

    public RelayCommand OpenCsvCommand { get; }

    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                ApplyFilter();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

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
                Symbol = s.Symbol
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

    private void ApplyFilter()
    {
        var keyword = SearchKeyword.Trim();

        var filtered = string.IsNullOrWhiteSpace(keyword)
            ? _allStocks
            : _allStocks.Where(s =>
                s.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

        Stocks.Clear();
        foreach (var item in filtered)
        {
            Stocks.Add(item);
        }
    }
}
