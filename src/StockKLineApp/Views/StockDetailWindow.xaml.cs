using StockKLineApp.ViewModels;

namespace StockKLineApp.Views;

public partial class StockDetailWindow : System.Windows.Window
{
    public StockDetailWindow(StockListItemViewModel stock)
    {
        InitializeComponent();
        DataContext = new StockDetailViewModel(stock);
    }
}

public sealed class StockDetailViewModel
{
    public StockDetailViewModel(StockListItemViewModel stock)
    {
        CodeLabel = $"代码：{stock.Code}";
        NameLabel = $"名称：{stock.Name}";
        SymbolLabel = $"Symbol：{stock.Symbol}";
    }

    public string CodeLabel { get; }
    public string NameLabel { get; }
    public string SymbolLabel { get; }
}
