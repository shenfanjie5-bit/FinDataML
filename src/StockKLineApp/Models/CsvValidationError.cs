namespace StockKLineApp.Models;

public sealed class CsvValidationError
{
    public required int Row { get; init; }
    public required string ColumnName { get; init; }
    public required int ColumnIndex { get; init; }
    public required string RawValue { get; init; }
    public required string Reason { get; init; }

    public override string ToString()
        => $"CSV 解析失败：第 {Row} 行，第 {ColumnIndex} 列（{ColumnName}），值 \"{RawValue}\"，{Reason}";
}
