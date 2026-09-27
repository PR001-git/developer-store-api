using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
using Ambev.DeveloperEvaluation.ORM.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Services;

/// <summary>
/// Implementation of <see cref="ISaleNumberGenerator"/> on the PostgreSQL sequence <c>sale_number_seq</c> (spec §8.3).
/// </summary>
/// <remarks>
/// The sequence never returns a value twice, so generated numbers never collide with each other. A client may send a
/// number the sequence reaches later, and the generator skips it. The unique index stays the final guard against a
/// client sending the same number at the same moment.
/// </remarks>
public sealed class SaleNumberGenerator : ISaleNumberGenerator
{
    /// <summary>
    /// Reads the next value of the sequence. EF Core needs the column to be named <c>Value</c> to compose a query on it.
    /// </summary>
    private const string NextValueSql = "SELECT nextval('" + SaleConfiguration.SaleNumberSequence + "') AS \"Value\"";

    private readonly DefaultContext _context;
    private readonly ISaleRepository _saleRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaleNumberGenerator"/> class.
    /// </summary>
    /// <param name="context">The database context, for the sequence.</param>
    /// <param name="saleRepository">The sale repository, to skip numbers that are taken.</param>
    public SaleNumberGenerator(DefaultContext context, ISaleRepository saleRepository)
    {
        _context = context;
        _saleRepository = saleRepository;
    }

    /// <inheritdoc />
    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        string candidate;
        do
        {
            var value = await _context.Database.SqlQueryRaw<long>(NextValueSql).SingleAsync(cancellationToken);
            candidate = $"S-{value:D6}";
        }
        while (await _saleRepository.ExistsBySaleNumberAsync(candidate, cancellationToken));

        return candidate;
    }
}
