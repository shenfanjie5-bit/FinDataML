using System.Windows;
using StockKLineApp.Services;
using StockKLineApp.ViewModels;
using StockKLineApp.Views;

namespace StockKLineApp;

public partial class MainWindow : Window
{
    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        MainPage mainPage)
    {
        InitializeComponent();

        DataContext = viewModel;
        navigationService.Initialize(RootFrame);
        navigationService.Navigate(mainPage);
    }
}
