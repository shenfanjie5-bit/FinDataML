using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StockKLineApp.Services;
using StockKLineApp.ViewModels;
using StockKLineApp.Views;

namespace StockKLineApp;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<INavigationService, FrameNavigationService>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainPage>();
        services.AddSingleton<MainPageViewModel>();
        services.AddSingleton<StockDetailPage>();
        services.AddSingleton<StockDetailPageViewModel>();
    }
}
