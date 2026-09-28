namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Reads <c>_minSaleDate</c> and <c>_maxSaleDate</c> (spec §7.3) as inclusive UTC bounds. A value without an offset
/// is UTC; the model binder has already converted a value with an offset to UTC. A max at exactly midnight UTC covers
/// that whole day.
/// </summary>
public static class SaleDateBounds
{
    // PostgreSQL stores timestamptz to the microsecond, so a day's last microsecond is an exact inclusive end.
    // Adding a day less a microsecond, rather than a day, also keeps 9999-12-31 from overflowing.
    private const long DayLessOneMicrosecond = TimeSpan.TicksPerDay - TimeSpan.TicksPerMicrosecond;

    /// <summary>
    /// Returns the inclusive lower bound of a <c>_minSaleDate</c>: the same instant, in UTC.
    /// </summary>
    /// <param name="minSaleDate">The earliest sale date sent.</param>
    /// <returns>The lower bound, in UTC.</returns>
    public static DateTime LowerBound(DateTime minSaleDate) => ToUtc(minSaleDate);

    /// <summary>
    /// Returns the inclusive upper bound of a <c>_maxSaleDate</c>: the same instant in UTC or, when that is exactly
    /// midnight, the last microsecond of that day.
    /// </summary>
    /// <param name="maxSaleDate">The latest sale date sent.</param>
    /// <returns>The upper bound, in UTC.</returns>
    public static DateTime UpperBound(DateTime maxSaleDate)
    {
        var utc = ToUtc(maxSaleDate);
        return utc.TimeOfDay == TimeSpan.Zero ? utc.AddTicks(DayLessOneMicrosecond) : utc;
    }

    // Rule R13's kind rules, the ones Sale applies to SaleDate.
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
