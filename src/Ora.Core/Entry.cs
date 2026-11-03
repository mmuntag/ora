using System.Globalization;
using System.Text.RegularExpressions;

namespace Ora.Core;

public sealed record Entry(DateTime Start, DateTime? End, string Name)
{
    public static DateTime Seconds(DateTime value) => new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Kind);
    public static string NormalizeName(string value)
    {
        value = Regex.Replace(value.Trim(), @"\s+", " ");
        int colon = value.IndexOf(':');
        if (colon >= 0) value = value[..colon].Trim() + ": " + value[(colon + 1)..].Trim();
        if (value.Length == 0 || (colon >= 0 && (value.StartsWith(':') || value.EndsWith(' '))))
            throw new FormatException("A megnevezés nem lehet üres; a kettőspont mindkét oldalára írj szöveget.");
        return value;
    }

    public static IEnumerable<Entry> Split(DateTime start, DateTime end, string name)
    {
        if (end < start) throw new FormatException("A befejezés nem lehet korábbi a kezdésnél. Ellenőrizd a gép óráját.");
        while (start.Date < end.Date)
        {
            var midnight = start.Date.AddDays(1);
            yield return new(start, midnight, name);
            start = midnight;
        }
        if (start < end || start == end && end.TimeOfDay != TimeSpan.Zero)
            yield return new(start, end, name);
    }

    public string[] Fields() => [Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Start.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        End is null ? "" : End.Value.Date > Start.Date ? "24:00:00" : End.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture), Name];
}
