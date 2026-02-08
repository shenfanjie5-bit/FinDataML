using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockKLineApp.Models;
using StockKLineApp.Services;

namespace StockKLineApp.ViewModels;

public partial class StockDetailPageViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string pageTitle = "股票详情占位";

    [ObservableProperty]
    private string summaryText = "请选择一支股票查看详情。";

    public StockDetailPageViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void Load(StockSummary stock)
    {
        PageTitle = $"{stock.Symbol} - {stock.Name}";
        SummaryText = "这里将承载 K 线图、区间选择和统计信息。";
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
