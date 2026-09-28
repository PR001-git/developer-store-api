using System.Linq.Expressions;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.ORM.Mapping;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

/// <summary>
/// Implementation of <see cref="ISaleRepository"/> using Entity Framework Core.
/// </summary>
public sealed class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        _context.Sales.Add(sale);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsSaleNumberConflict(exception))
        {
            // Another request saved the same number after this one checked it; the unique index is the final guard (spec §8.2).
            throw new DomainException(Sale.DuplicateSaleNumberMessage(sale.SaleNumber), exception);
        }
    }

    /// <inheritdoc />
    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Sales
            .Include(sale => sale.Items)
            .FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(sale).State == EntityState.Detached)
            throw new InvalidOperationException("Only a sale loaded with GetByIdAsync can be updated");

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SalePage> ListAsync(SaleListQuery query, CancellationToken cancellationToken = default)
    {
        // Filtered before the count, so the total counts only the matching sales.
        var sales = ApplyFilter(_context.Sales.AsNoTracking(), query.Filter);

        var totalCount = await sales.CountAsync(cancellationToken);

        // Past the last sale there is nothing to read. Computing in long also keeps a huge page from overflowing Skip.
        var skip = (long)(query.Page - 1) * query.Size;
        if (skip >= totalCount)
            return new SalePage([], totalCount);

        var page = await ApplyOrder(sales.Include(sale => sale.Items).AsSplitQuery(), query.Sorts)
            .Skip((int)skip)
            .Take(query.Size)
            .ToListAsync(cancellationToken);

        return new SalePage(page, totalCount);
    }

    /// <summary>
    /// Keeps the sales that match every criterion the filter sets (spec §7.3). Text criteria use ILIKE with an escape
    /// character; every value is a parameter, never part of the SQL text.
    /// </summary>
    private static IQueryable<Sale> ApplyFilter(IQueryable<Sale> sales, SaleListFilter filter)
    {
        if (filter.SaleNumber is not null)
        {
            var pattern = LikePattern.FromWildcards(filter.SaleNumber);
            sales = sales.Where(sale => EF.Functions.ILike(sale.SaleNumber, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.CustomerName is not null)
        {
            var pattern = LikePattern.FromWildcards(filter.CustomerName);
            sales = sales.Where(sale => EF.Functions.ILike(sale.Customer.Name, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.BranchName is not null)
        {
            var pattern = LikePattern.FromWildcards(filter.BranchName);
            sales = sales.Where(sale => EF.Functions.ILike(sale.Branch.Name, pattern, LikePattern.EscapeCharacter));
        }

        if (filter.CustomerId is { } customerId)
            sales = sales.Where(sale => sale.Customer.Id == customerId);
        if (filter.BranchId is { } branchId)
            sales = sales.Where(sale => sale.Branch.Id == branchId);
        if (filter.IsCancelled is { } isCancelled)
            sales = sales.Where(sale => sale.IsCancelled == isCancelled);
        if (filter.MinSaleDate is { } minSaleDate)
            sales = sales.Where(sale => sale.SaleDate >= minSaleDate);
        if (filter.MaxSaleDate is { } maxSaleDate)
            sales = sales.Where(sale => sale.SaleDate <= maxSaleDate);
        if (filter.MinTotalAmount is { } minTotalAmount)
            sales = sales.Where(sale => sale.TotalAmount >= minTotalAmount);
        if (filter.MaxTotalAmount is { } maxTotalAmount)
            sales = sales.Where(sale => sale.TotalAmount <= maxTotalAmount);

        return sales;
    }

    /// <summary>
    /// Orders by each sort in turn, then by id. Each field maps to a fixed expression; nothing is looked up by name.
    /// </summary>
    private static IOrderedQueryable<Sale> ApplyOrder(IQueryable<Sale> sales, IReadOnlyList<SaleSort> sorts)
    {
        IOrderedQueryable<Sale>? ordered = null;
        foreach (var sort in sorts)
        {
            ordered = sort.Field switch
            {
                SaleSortField.SaleNumber => AppendOrdering(sales, ordered, sale => sale.SaleNumber, sort.Descending),
                SaleSortField.SaleDate => AppendOrdering(sales, ordered, sale => sale.SaleDate, sort.Descending),
                SaleSortField.CustomerName => AppendOrdering(sales, ordered, sale => sale.Customer.Name, sort.Descending),
                SaleSortField.BranchName => AppendOrdering(sales, ordered, sale => sale.Branch.Name, sort.Descending),
                SaleSortField.TotalAmount => AppendOrdering(sales, ordered, sale => sale.TotalAmount, sort.Descending),
                SaleSortField.IsCancelled => AppendOrdering(sales, ordered, sale => sale.IsCancelled, sort.Descending),
                _ => throw new ArgumentOutOfRangeException(nameof(sorts), sort.Field, "The sort field has no ordering expression.")
            };
        }

        return ordered is null ? sales.OrderBy(sale => sale.Id) : ordered.ThenBy(sale => sale.Id);
    }

    private static IOrderedQueryable<Sale> AppendOrdering<TKey>(
        IQueryable<Sale> sales, IOrderedQueryable<Sale>? ordered, Expression<Func<Sale, TKey>> key, bool descending) =>
        (ordered, descending) switch
        {
            (null, false) => sales.OrderBy(key),
            (null, true) => sales.OrderByDescending(key),
            ({ } current, false) => current.ThenBy(key),
            ({ } current, true) => current.ThenByDescending(key)
        };

    /// <inheritdoc />
    public Task<bool> ExistsBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default) =>
        _context.Sales.AnyAsync(sale => sale.SaleNumber == saleNumber, cancellationToken);

    private static bool IsSaleNumberConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: SaleConfiguration.SaleNumberIndex
        };
}
