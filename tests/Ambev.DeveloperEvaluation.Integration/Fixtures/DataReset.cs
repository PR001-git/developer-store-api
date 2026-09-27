using Npgsql;

namespace Ambev.DeveloperEvaluation.Integration.Fixtures;

/// <summary>
/// Builds and runs the reset statements <see cref="PostgreSqlFixture.ResetDataAsync"/> (Integration) and
/// <c>ApiFixture.ResetDataAsync</c> (Functional) share. Functional links this file in rather than
/// duplicating it; see its csproj.
/// </summary>
internal static class DataReset
{
    /// <summary>
    /// Empties the given tables and restarts the given sequences.
    /// </summary>
    /// <param name="connectionString">The database to reset.</param>
    /// <param name="tables">
    /// Tables to truncate. <c>RESTART IDENTITY CASCADE</c> also resets their identity columns and empties
    /// any table that references them through a foreign key but isn't itself in this list.
    /// </param>
    /// <param name="sequences">Sequences to restart from their start value.</param>
    public static async Task RunAsync(string connectionString, IReadOnlyCollection<string> tables, IReadOnlyCollection<string> sequences)
    {
        var statements = new List<string>();
        if (tables.Count > 0)
            statements.Add($"TRUNCATE TABLE {string.Join(", ", tables.Select(Quote))} RESTART IDENTITY CASCADE;");
        statements.AddRange(sequences.Select(sequence => $"ALTER SEQUENCE {Quote(sequence)} RESTART;"));

        if (statements.Count == 0)
            return;

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var command = dataSource.CreateCommand(string.Join(Environment.NewLine, statements));
        await command.ExecuteNonQueryAsync();
    }

    private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
