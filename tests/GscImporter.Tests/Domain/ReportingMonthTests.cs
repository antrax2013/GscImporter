using GscImporter.Domain;

namespace GscImporter.Tests.Domain;

public sealed class ReportingMonthTests
{
    [Fact]
    public void FromDates_AcceptsACompleteCalendarMonth()
    {
        var dates = Enumerable.Range(1, 31).Select(day => new DateOnly(2026, 1, day)).ToArray();
        Assert.Equal(new ReportingMonth(2026, 1), ReportingMonth.FromDates(dates));
    }

    [Fact]
    public void FromDates_RejectsAnIncompleteCalendarMonth()
    {
        var dates = Enumerable.Range(1, 30).Select(day => new DateOnly(2026, 1, day)).ToArray();
        Assert.Throws<InvalidDataException>(() => ReportingMonth.FromDates(dates));
    }
}
