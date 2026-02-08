using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using StockKLineApp.Models;

namespace StockKLineApp.Services;

public sealed class CsvStockImportService
{
    private static readonly Dictionary<string, string[]> ColumnAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Code"] = ["股票代码", "code"],
        ["Name"] = ["名称", "name"],
        ["Symbol"] = ["symbol", "股票标识"],
        ["Date"] = ["日期", "date"],
        ["Open"] = ["今开", "open"],
        ["Close"] = ["今末", "close"],
        ["High"] = ["最高", "high"],
        ["Low"] = ["最低", "low"],
        ["TurnoverWan"] = ["成交额", "turnover", "amount"]
    };

    public CsvImportResult Import(string path, IProgress<ImportProgressInfo>? progress = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        progress?.Report(new ImportProgressInfo { Percent = 0, Message = "开始解析 UTF-8..." });

        var errors = new List<CsvValidationError>();
        var data = TryImport(path, new UTF8Encoding(false, true), "UTF-8", errors, progress);
        if (data is not null)
        {
            return data;
        }

        progress?.Report(new ImportProgressInfo { Percent = 0, Message = "UTF-8 失败，尝试 GBK..." });

        errors.Clear();
        data = TryImport(path, Encoding.GetEncoding("GBK"), "GBK", errors, progress);
        if (data is null)
        {
            throw new InvalidOperationException("无法解析 CSV 文件，请确认编码和数据格式。");
        }

        return data;
    }

    private static CsvImportResult? TryImport(string path, Encoding encoding, string encodingName, List<CsvValidationError> errors, IProgress<ImportProgressInfo>? progress)
    {
        try
        {
            var totalRows = Math.Max(1, File.ReadLines(path, encoding).Skip(1).Count());

            using var reader = new StreamReader(path, encoding, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            });

            if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord is null)
            {
                return new CsvImportResult
                {
                    Stocks = [],
                    Errors = [],
                    EncodingUsed = encodingName
                };
            }

            var header = csv.HeaderRecord;
            var columnMap = ResolveColumns(header);
            var grouped = new Dictionary<string, StockSeries>(StringComparer.OrdinalIgnoreCase);

            while (csv.Read())
            {
                var row = csv.Parser.Row;
                var parsedRowIndex = Math.Max(1, row - 1);

                if (parsedRowIndex % 200 == 0)
                {
                    var percent = Math.Clamp((int)Math.Round(parsedRowIndex * 100.0 / totalRows), 0, 99);
                    progress?.Report(new ImportProgressInfo { Percent = percent, Message = $"解析中... {parsedRowIndex}/{totalRows}" });
                }

                if (!TryGetRequiredString(csv, header, columnMap, "Code", row, errors, out var code) ||
                    !TryGetRequiredString(csv, header, columnMap, "Name", row, errors, out var name) ||
                    !TryGetRequiredString(csv, header, columnMap, "Symbol", row, errors, out var symbol) ||
                    !TryGetDate(csv, header, columnMap, row, errors, out var date) ||
                    !TryGetDecimal(csv, header, columnMap, "Open", row, errors, out var open) ||
                    !TryGetDecimal(csv, header, columnMap, "Close", row, errors, out var close) ||
                    !TryGetDecimal(csv, header, columnMap, "High", row, errors, out var high) ||
                    !TryGetDecimal(csv, header, columnMap, "Low", row, errors, out var low))
                {
                    continue;
                }

                _ = TryGetNullableTurnoverWan(csv, header, columnMap, "TurnoverWan", row, errors, out var turnoverWan);

                var key = $"{code}|{name}";
                if (!grouped.TryGetValue(key, out var series))
                {
                    series = new StockSeries { Code = code, Name = name, Symbol = symbol };
                    grouped[key] = series;
                }

                series.Bars.Add(new DailyBar
                {
                    Date = date,
                    Open = open,
                    Close = close,
                    High = high,
                    Low = low,
                    TurnoverWan = turnoverWan
                });
            }

            var stocks = grouped.Values
                .Select(s =>
                {
                    s.Bars.Sort((a, b) => a.Date.CompareTo(b.Date));
                    return s;
                })
                .OrderBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new CsvImportResult
            {
                Stocks = stocks,
                Errors = errors.ToList(),
                EncodingUsed = encodingName
            };
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    private static Dictionary<string, int> ResolveColumns(string[] header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Length; i++)
        {
            var h = header[i].Trim();
            foreach (var (canonical, aliases) in ColumnAliases)
            {
                if (aliases.Any(a => string.Equals(a, h, StringComparison.OrdinalIgnoreCase)))
                {
                    map[canonical] = i;
                }
            }
        }

        return map;
    }

    private static bool TryGetRequiredString(CsvReader csv, string[] header, Dictionary<string, int> map, string canonical, int row, List<CsvValidationError> errors, out string value)
    {
        value = string.Empty;
        if (!TryGetRaw(csv, header, map, canonical, out var raw, out var idx, out var name))
        {
            errors.Add(new CsvValidationError { Row = row, ColumnName = canonical, ColumnIndex = -1, RawValue = string.Empty, Reason = "缺少必需列。" });
            return false;
        }

        value = raw.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new CsvValidationError { Row = row, ColumnName = name, ColumnIndex = idx + 1, RawValue = raw, Reason = "必填字段为空。" });
            return false;
        }

        return true;
    }

    private static bool TryGetDate(CsvReader csv, string[] header, Dictionary<string, int> map, int row, List<CsvValidationError> errors, out DateOnly date)
    {
        date = default;
        if (!TryGetRaw(csv, header, map, "Date", out var raw, out var idx, out var name))
        {
            errors.Add(new CsvValidationError { Row = row, ColumnName = "Date", ColumnIndex = -1, RawValue = string.Empty, Reason = "缺少必需列。" });
            return false;
        }

        var value = raw.Trim();
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
            DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        errors.Add(new CsvValidationError { Row = row, ColumnName = name, ColumnIndex = idx + 1, RawValue = raw, Reason = "日期格式非法，需为 YYYY-MM-DD 或 YYYYMMDD。" });
        return false;
    }

    private static bool TryGetDecimal(CsvReader csv, string[] header, Dictionary<string, int> map, string canonical, int row, List<CsvValidationError> errors, out decimal value)
    {
        value = default;
        if (!TryGetRaw(csv, header, map, canonical, out var raw, out var idx, out var name))
        {
            errors.Add(new CsvValidationError { Row = row, ColumnName = canonical, ColumnIndex = -1, RawValue = string.Empty, Reason = "缺少必需列。" });
            return false;
        }

        if (decimal.TryParse(raw.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        errors.Add(new CsvValidationError { Row = row, ColumnName = name, ColumnIndex = idx + 1, RawValue = raw, Reason = "无法解析为 decimal。" });
        return false;
    }

    private static bool TryGetNullableTurnoverWan(CsvReader csv, string[] header, Dictionary<string, int> map, string canonical, int row, List<CsvValidationError> errors, out decimal? value)
    {
        value = null;
        if (!TryGetRaw(csv, header, map, canonical, out var raw, out var idx, out var name))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!decimal.TryParse(raw.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            errors.Add(new CsvValidationError { Row = row, ColumnName = name, ColumnIndex = idx + 1, RawValue = raw, Reason = "无法解析为 decimal。" });
            return false;
        }

        if (decimal.Round(parsed, 6) != parsed)
        {
            errors.Add(new CsvValidationError { Row = row, ColumnName = name, ColumnIndex = idx + 1, RawValue = raw, Reason = "最多支持 6 位小数。" });
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryGetRaw(CsvReader csv, string[] header, Dictionary<string, int> map, string canonical, out string value, out int index, out string columnName)
    {
        value = string.Empty;
        index = -1;
        columnName = canonical;

        if (!map.TryGetValue(canonical, out index))
        {
            return false;
        }

        columnName = header[index];
        value = csv.GetField(index) ?? string.Empty;
        return true;
    }
}
