using System.Globalization;

namespace GscImporter.Domain;

public readonly record struct ReportingMonth
{
    public ReportingMonth(int year, int month)
    {
        if (year < 2000 || year > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        Year = year;
        Month = month;
    }

    public int Year { get; }
    public int Month { get; }
    public DateOnly FirstDay => new(Year, Month, 1);
    public DateOnly LastDay => new(Year, Month, DateTime.DaysInMonth(Year, Month));
    public override string ToString() => $"{Year:D4}-{Month:D2}";

    public static ReportingMonth FromDates(IReadOnlyCollection<DateOnly> dates)
    {
        if (dates.Count == 0) throw new InvalidDataException("Graphique.csv contains no reporting date.");
        var uniqueDates = dates.Distinct().Order().ToArray();
        var first = uniqueDates[0];
        var last = uniqueDates[^1];
        var month = new ReportingMonth(first.Year, first.Month);
        if (first != month.FirstDay ||
            last != month.LastDay ||
            uniqueDates.Length != DateTime.DaysInMonth(month.Year, month.Month) ||
            uniqueDates.Any(date => date.Year != month.Year || date.Month != month.Month))
            throw new InvalidDataException("The GSC export must cover one complete calendar month.");
        return month;
    }
}
