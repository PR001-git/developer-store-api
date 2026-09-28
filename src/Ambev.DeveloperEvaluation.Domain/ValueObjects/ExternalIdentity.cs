using Ambev.DeveloperEvaluation.Domain.Exceptions;

namespace Ambev.DeveloperEvaluation.Domain.ValueObjects;

/// <summary>
/// An entity that belongs to another domain (a customer, a branch or a product), referenced by its id
/// together with a copy of its name. The name is a snapshot taken when the sale is written.
/// </summary>
/// <remarks>
/// A value object: two identities are equal when their id and name are equal.
/// EF Core maps it as an owned type, into columns of the table that owns it.
/// </remarks>
public sealed record ExternalIdentity
{
    /// <summary>
    /// The maximum length of <see cref="Name"/>, after trimming.
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalIdentity"/> record.
    /// </summary>
    /// <param name="id">The id of the entity in its own domain. Must not be empty.</param>
    /// <param name="name">The name of the entity. Trimmed, it must have 1 to 100 characters.</param>
    /// <exception cref="DomainException">Thrown when the id is empty or the name is missing, blank or too long.</exception>
    public ExternalIdentity(Guid id, string name)
    {
        if (id == Guid.Empty)
            throw new DomainException("External identity id must not be empty");

        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length is 0 or > NameMaxLength)
            throw new DomainException($"External identity name must have 1 to {NameMaxLength} characters");

        Id = id;
        Name = trimmedName;
    }

    /// <summary>
    /// Gets the id of the entity in its own domain.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the name of the entity, trimmed.
    /// </summary>
    public string Name { get; }
}
