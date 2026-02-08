using System.Windows.Controls;

namespace StockKLineApp.Services;

public class FrameNavigationService : INavigationService
{
    private Frame? _frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public void Navigate(Page page)
    {
        _frame?.Navigate(page);
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            _frame?.GoBack();
        }
    }
}
