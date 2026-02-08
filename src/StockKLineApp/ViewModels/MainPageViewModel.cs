using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockKLineApp.Models;
using StockKLineApp.Services;
using StockKLineApp.Views;

namespace StockKLineApp.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly StockDetailPage _stockDetailPage;
    private readonly StockDetailPageViewModel _stockDetailPageViewModel;

    [ObservableProperty]
    private string searchKeyword = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenStockDetailCommand))]
    private StockSummary? selectedStock;

    public ObservableCollection<StockSummary> Stocks { get; }

    public ICollectionView FilteredStocks { get; }

    public MainPageViewModel(
        INavigationService navigationService,
        StockDetailPage stockDetailPage,
        StockDetailPageViewModel stockDetailPageViewModel)
    {
        _navigationService = navigationService;
        _stockDetailPage = stockDetailPage;
        _stockDetailPageViewModel = stockDetailPageViewModel;

        Stocks = new ObservableCollection<StockSummary>
        {
            new() { Symbol = "AAPL", Name = "Apple Inc." },
            new() { Symbol = "MSFT", Name = "Microsoft Corp." },
            new() { Symbol = "TSLA", Name = "Tesla, Inc." }
        };

        FilteredStocks = CollectionViewSource.GetDefaultView(Stocks);
        FilteredStocks.Filter = FilterStock;

        Stocks.CollectionChanged += OnStocksCollectionChanged;
    }

    [RelayCommand(CanExecute = nameof(CanOpenStockDetail))]
    private void OpenStockDetail()
    {
        if (SelectedStock is null)
        {
            return;
        }

        _stockDetailPageViewModel.Load(SelectedStock);
        _navigationService.Navigate(_stockDetailPage);
    }

    partial void OnSearchKeywordChanged(string value)
    {
        FilteredStocks.Refresh();
    }

    private bool FilterStock(object item)
    {
        if (item is not StockSummary stock)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchKeyword))
        {
            return true;
        }

        return stock.Symbol.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase)
               || stock.Name.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase);
    }

    private bool CanOpenStockDetail() => SelectedStock is not null;

    private void OnStocksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove && SelectedStock is not null && !Stocks.Contains(SelectedStock))
        {
            SelectedStock = null;
        }
    }
}
