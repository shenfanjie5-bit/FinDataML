using System.Windows.Controls;
using StockKLineApp.ViewModels;

namespace StockKLineApp.Views;

public partial class StockDetailPage : Page
{
    public StockDetailPage(StockDetailPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
