using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;

namespace Ora.Core;

public sealed class Store(string directory)
{
    public const string Header = "Kezdődátum,kezdőidő,vége idő,megnevezés";
    public string DirectoryPath { get; } = directory;
    public string DataPath => Path.Combine(DirectoryPath, "idomeres.csv");
    public string ReportPath => Path.Combine(DirectoryPath, "kimutatas.html");
    public string CheckpointPath => Path.Combine(DirectoryPath, "folyamat.json");

    public List<Entry> Read()
    {
        Directory.CreateDirectory(DirectoryPath);
        if (!File.Exists(DataPath)) return [];
        using var parser = new TextFieldParser(DataPath, Encoding.UTF8) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = true };
        parser.SetDelimiters(",");
        List<Entry> result = [];
        while (!parser.EndOfData)
        {
            long line = parser.LineNumber;
            string[] fields;
            try { fields = parser.ReadFields()!; }
            catch (MalformedLineException ex) { throw new FormatException($"Hibás CSV-idézőjelezés a(z) {line}. sorban. A fájl nem lett felülírva.", ex); }
            if (fields.All(string.IsNullOrWhiteSpace)) continue;
            if (fields[0].Trim().Equals("Kezdődátum", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                if (fields.Length < 4) throw new FormatException("Négy mező szükséges.");
                var date = DateTime.ParseExact(fields[0].Trim(), ["yyyy-MM-dd", "yyyy-M-d", "yyyy.MM.dd", "yyyy.MM.dd."], CultureInfo.InvariantCulture, DateTimeStyles.None);
                var start = date.Add(ParseTime(fields[1], false));
                DateTime? end = string.IsNullOrWhiteSpace(fields[2]) ? null : date.Add(ParseTime(fields[2], true));
                if (end < start) end = end.Value.AddDays(1);
                var name = Entry.NormalizeName(string.Join(",", fields.Skip(3)));
                if (end is null) result.Add(new(start, null, name));
                else if (end == start) result.Add(new(start, end, name));
                else result.AddRange(Entry.Split(start, end.Value, name));
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException)
            { throw new FormatException($"Hibás CSV-sor ({line}): {ex.Message} A fájl nem lett felülírva.", ex); }
        }
        return result;
    }

    private static TimeSpan ParseTime(string text, bool allowMidnight)
    {
        text = text.Trim();
        if (allowMidnight && text == "24:00:00") return TimeSpan.FromDays(1);
        if (TimeOnly.TryParseExact(text, ["H:mm:ss", "HH:mm:ss", "H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return time.ToTimeSpan();
        throw new FormatException($"Érvénytelen idő: {text}");
    }

    public void Write(IReadOnlyList<Entry> entries)
    {
        string csv = Header + "\r\n" + string.Join("\r\n", entries.OrderBy(e => e.Start).Select(e => string.Join(",", e.Fields().Select(Quote)))) + (entries.Count > 0 ? "\r\n" : "");
        // Preserve the previous manual file before normalization. Never keep a file handle open between operations.
        if (File.Exists(DataPath) && File.ReadAllText(DataPath) != csv)
            File.Copy(DataPath, DataPath + ".bak", true);
        AtomicWrite(DataPath, csv);
        Report(entries);
    }

    public void Report(IReadOnlyList<Entry> entries)
    {
        using var stream = typeof(Store).Assembly.GetManifestResourceStream("Ora.Core.report.html")!;
        using var reader = new StreamReader(stream);
        var data = entries.Select(e => new { date = e.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), start = e.Start.ToString("O"), end = e.End?.ToString("O"), name = e.Name });
        AtomicWrite(ReportPath, reader.ReadToEnd().Replace("/*DATA*/[]", JsonSerializer.Serialize(data)));
    }

    public static void AtomicWrite(string path, string text)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = new UTF8Encoding(false).GetBytes(text);
                file.Write(bytes);
                file.Flush(true);
            }
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static string Quote(string text) => text.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
    public void Checkpoint(Entry entry, DateTime now) => AtomicWrite(CheckpointPath, JsonSerializer.Serialize(new Checkpoint(entry.Start, entry.Name, now)));
    public Checkpoint? ReadCheckpoint()
    {
        try { return File.Exists(CheckpointPath) ? JsonSerializer.Deserialize<Checkpoint>(File.ReadAllText(CheckpointPath)) : null; }
        catch (JsonException) { return null; }
    }
}

public sealed record Checkpoint(DateTime Start, string Name, DateTime LastSeen);
