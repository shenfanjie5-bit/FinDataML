using System.Windows.Controls;

namespace StockKLineApp.Services;

public interface INavigationService
{
    void Initialize(Frame frame);

    void Navigate(Page page);

    void GoBack();

    bool CanGoBack { get; }
}
