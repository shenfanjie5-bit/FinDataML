using StockKLineApp.Services;
using StockKLineApp.ViewModels;

namespace StockKLineApp.Views;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new MainViewModel();
        viewModel.NavigateToStockRequested += NavigateToStockDetail;

        DataContext = viewModel;
    }

    private void NavigateToStockDetail(StockListItemViewModel stock, StockDataCacheService cache)
    {
        var detailWindow = new StockDetailWindow(stock, cache)
        {
            Owner = this
        };

        detailWindow.Show();
    }
}
