namespace StockKLineApp.Models;

public sealed class CsvImportResult
{
    public required IReadOnlyList<StockSeries> Stocks { get; init; }
    public required IReadOnlyList<CsvValidationError> Errors { get; init; }
    public required string EncodingUsed { get; init; }
}
