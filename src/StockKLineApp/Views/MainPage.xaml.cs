using System.Windows.Controls;
using System.Windows.Input;
using StockKLineApp.ViewModels;

namespace StockKLineApp.Views;

public partial class MainPage : Page
{
    private readonly MainPageViewModel _viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnStockDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.OpenStockDetailCommand.CanExecute(null))
        {
            _viewModel.OpenStockDetailCommand.Execute(null);
        }
    }
}
