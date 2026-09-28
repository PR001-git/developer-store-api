using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.ORM;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ambev.DeveloperEvaluation.Functional.Fixtures;

/// <summary>
/// Contains tests for <see cref="ApiFixture.ResetDataAsync"/>, which runs after every test class.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class DataResetTests
{
    private readonly ApiFixture _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataResetTests"/> class.
    /// </summary>
    /// <param name="api">The collection's API, injected by xUnit.</param>
    public DataResetTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>
    /// Tests that the reset empties the tables listed in <see cref="DataResetFixture.Tables"/>.
    /// </summary>
    [Fact(DisplayName = "Given a user saved through the API's services When the data is reset Then the Users table is empty")]
    public async Task Given_SavedUser_When_DataIsReset_Then_UsersTableIsEmpty()
    {
        // Given
        await using (var scope = _api.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            await users.CreateAsync(new User
            {
                Username = "reset.probe",
                Email = "reset.probe@example.com",
                Phone = "+5511999999999",
                Password = "Test@123",
                Role = UserRole.Customer,
                Status = UserStatus.Active,
            });
        }

        // When
        await _api.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        // Then
        await using var readScope = _api.Services.CreateAsyncScope();
        var context = readScope.ServiceProvider.GetRequiredService<DefaultContext>();
        (await context.Users.CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// Tests that the reset empties the Sales tables too, items included.
    /// </summary>
    [Fact(DisplayName = "Given a sale saved through the API's services When the data is reset Then the Sales and SaleItems tables are empty")]
    public async Task Given_SavedSale_When_DataIsReset_Then_SalesTablesAreEmpty()
    {
        // Given
        await using (var scope = _api.Services.CreateAsyncScope())
        {
            var sales = scope.ServiceProvider.GetRequiredService<ISaleRepository>();
            await sales.CreateAsync(Sale.Create(
                "S-RESET-PROBE",
                DateTime.UtcNow,
                new ExternalIdentity(Guid.NewGuid(), "Reset Probe"),
                new ExternalIdentity(Guid.NewGuid(), "Filial Probe"),
                [new SaleItemData(new ExternalIdentity(Guid.NewGuid(), "Probe Product"), 1, 1.00m)]));
        }

        // When
        await _api.ResetDataAsync(DataResetFixture.Tables, DataResetFixture.Sequences);

        // Then
        await using var readScope = _api.Services.CreateAsyncScope();
        var context = readScope.ServiceProvider.GetRequiredService<DefaultContext>();
        (await context.Sales.CountAsync()).Should().Be(0);
        (await context.Set<SaleItem>().CountAsync()).Should().Be(0);
    }
}
