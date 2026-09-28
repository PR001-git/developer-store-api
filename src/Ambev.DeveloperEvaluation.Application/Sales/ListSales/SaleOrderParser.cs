using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

/// <summary>
/// Parses the <c>_order</c> parameter of the sales list (spec §7.3): comma-separated <c>field [asc|desc]</c>
/// clauses, with or without surrounding double quotes. Names and directions ignore case; the direction defaults to asc.
/// </summary>
public static class SaleOrderParser
{
    /// <summary>
    /// The order when <c>_order</c> is omitted: newest sale first.
    /// </summary>
    public static readonly IReadOnlyList<SaleSort> DefaultOrder = [new SaleSort(SaleSortField.SaleDate, Descending: true)];

    private const string EmptyClauseMessage = "Each comma-separated sort clause needs a field.";

    private const string SortableFieldsHint =
        "Use saleNumber, saleDate, customerName, branchName, totalAmount or isCancelled.";

    // The JSON field names of spec §7.3. The id isn't here: it breaks ties, it is never a requested order.
    private static readonly FrozenDictionary<string, SaleSortField> FieldsByName =
        new Dictionary<string, SaleSortField>
        {
            ["saleNumber"] = SaleSortField.SaleNumber,
            ["saleDate"] = SaleSortField.SaleDate,
            ["customerName"] = SaleSortField.CustomerName,
            ["branchName"] = SaleSortField.BranchName,
            ["totalAmount"] = SaleSortField.TotalAmount,
            ["isCancelled"] = SaleSortField.IsCancelled
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Parses an order. A missing, blank or <c>""</c> order is <see cref="DefaultOrder"/>.
    /// </summary>
    /// <param name="order">The raw <c>_order</c> value.</param>
    /// <param name="sorts">The order, or an empty list when parsing fails.</param>
    /// <param name="error">Why parsing failed, or <c>null</c> when it succeeds.</param>
    /// <returns><c>true</c> if the order is valid.</returns>
    public static bool TryParse(string? order, out IReadOnlyList<SaleSort> sorts, [NotNullWhen(false)] out string? error)
    {
        var text = Unquote(order);
        if (string.IsNullOrWhiteSpace(text))
        {
            sorts = DefaultOrder;
            error = null;
            return true;
        }

        var parsed = new List<SaleSort>();
        foreach (var clause in text.Split(','))
        {
            error = ParseClause(clause, parsed);
            if (error is not null)
            {
                sorts = [];
                return false;
            }
        }

        sorts = parsed;
        error = null;
        return true;
    }

    /// <summary>
    /// Parses an order that the validator has already accepted.
    /// </summary>
    /// <param name="order">The raw <c>_order</c> value.</param>
    /// <returns>The order.</returns>
    /// <exception cref="FormatException">Thrown when the order is invalid, which means validation was skipped.</exception>
    public static IReadOnlyList<SaleSort> Parse(string? order) =>
        TryParse(order, out var sorts, out var error) ? sorts : throw new FormatException(error);

    private static string Unquote(string? order)
    {
        var text = order?.Trim() ?? string.Empty;
        return text.Length >= 2 && text[0] == '"' && text[^1] == '"' ? text[1..^1] : text;
    }

    private static string? ParseClause(string clause, List<SaleSort> parsed)
    {
        // A null separator splits on any whitespace, so repeated spaces and tabs are fine.
        var tokens = clause.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return EmptyClauseMessage;
        if (tokens.Length > 2)
            return $"'{clause.Trim()}' must be a field, optionally followed by asc or desc.";
        if (!FieldsByName.TryGetValue(tokens[0], out var field))
            return $"'{tokens[0]}' is not a sortable field. {SortableFieldsHint}";

        var direction = tokens.Length == 2 ? tokens[1] : "asc";
        var descending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);
        if (!descending && !direction.Equals("asc", StringComparison.OrdinalIgnoreCase))
            return $"'{direction}' is not a sort direction. Use asc or desc.";
        if (parsed.Exists(sort => sort.Field == field))
            return $"'{tokens[0]}' appears more than once.";

        parsed.Add(new SaleSort(field, descending));
        return null;
    }
}
