using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.ValueObjects;

/// <summary>
/// Contains unit tests for the <see cref="ExternalIdentity"/> value object.
/// </summary>
public sealed class ExternalIdentityTests
{
    /// <summary>
    /// Tests that the identity keeps its id and stores the name without surrounding spaces.
    /// </summary>
    [Fact(DisplayName = "Given an id and a name with surrounding spaces When creating an external identity Then it keeps the id and trims the name")]
    public void Given_IdAndNameWithSpaces_When_Creating_Then_KeepsIdAndTrimsName()
    {
        // Given
        var id = Guid.NewGuid();

        // When
        var identity = new ExternalIdentity(id, "  Maria Silva  ");

        // Then
        identity.Id.Should().Be(id);
        identity.Name.Should().Be("Maria Silva");
    }

    /// <summary>
    /// Tests that an empty id is rejected.
    /// </summary>
    [Fact(DisplayName = "Given an empty id When creating an external identity Then it throws DomainException")]
    public void Given_EmptyId_When_Creating_Then_ThrowsDomainException()
    {
        // When
        var act = () => new ExternalIdentity(Guid.Empty, "Maria Silva");

        // Then
        act.Should().Throw<DomainException>().WithMessage("External identity id must not be empty");
    }

    /// <summary>
    /// Tests that a missing or blank name is rejected.
    /// </summary>
    /// <param name="name">The name to try.</param>
    [Theory(DisplayName = "Given a missing or blank name When creating an external identity Then it throws DomainException")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_MissingOrBlankName_When_Creating_Then_ThrowsDomainException(string? name)
    {
        // When
        var act = () => new ExternalIdentity(Guid.NewGuid(), name!);

        // Then
        act.Should().Throw<DomainException>().WithMessage("External identity name must have 1 to 100 characters");
    }

    /// <summary>
    /// Tests that a name longer than 100 characters after trimming is rejected.
    /// </summary>
    [Fact(DisplayName = "Given a name of 101 characters When creating an external identity Then it throws DomainException")]
    public void Given_NameOf101Characters_When_Creating_Then_ThrowsDomainException()
    {
        // When
        var act = () => new ExternalIdentity(Guid.NewGuid(), new string('a', 101));

        // Then
        act.Should().Throw<DomainException>().WithMessage("External identity name must have 1 to 100 characters");
    }

    /// <summary>
    /// Tests that the length limit applies after trimming, so 100 characters plus spaces are accepted.
    /// </summary>
    [Fact(DisplayName = "Given a name of 100 characters with surrounding spaces When creating an external identity Then it is accepted")]
    public void Given_NameOf100CharactersWithSpaces_When_Creating_Then_IsAccepted()
    {
        // Given
        var name = new string('a', 100);

        // When
        var identity = new ExternalIdentity(Guid.NewGuid(), $"  {name}  ");

        // Then
        identity.Name.Should().Be(name);
    }

    /// <summary>
    /// Tests that two identities with the same id and name are equal, as value objects are.
    /// </summary>
    [Fact(DisplayName = "Given two external identities with the same id and name When comparing them Then they are equal")]
    public void Given_SameIdAndName_When_Comparing_Then_AreEqual()
    {
        // Given
        var id = Guid.NewGuid();

        // When
        var first = new ExternalIdentity(id, "Filial Centro");
        var second = new ExternalIdentity(id, " Filial Centro ");

        // Then
        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    /// <summary>
    /// Tests that identities that differ in the name are not equal.
    /// </summary>
    [Fact(DisplayName = "Given two external identities with the same id and different names When comparing them Then they are not equal")]
    public void Given_SameIdDifferentNames_When_Comparing_Then_AreNotEqual()
    {
        // Given
        var id = Guid.NewGuid();

        // When
        var first = new ExternalIdentity(id, "Filial Centro");
        var second = new ExternalIdentity(id, "Filial Norte");

        // Then
        first.Should().NotBe(second);
    }
}
