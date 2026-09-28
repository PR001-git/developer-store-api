using System.Text;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

/// <summary>
/// Turns a text filter of the sales list (spec §7.3) into an ILIKE pattern. A leading <c>*</c> and a trailing <c>*</c>
/// become <c>%</c>. Every other character matches itself: <c>%</c>, <c>_</c> and the escape character are escaped,
/// and any other <c>*</c> is already literal in ILIKE.
/// </summary>
public static class LikePattern
{
    /// <summary>
    /// The escape character of every pattern this class builds, for ILIKE's <c>ESCAPE</c> clause.
    /// </summary>
    public const string EscapeCharacter = @"\";

    /// <summary>
    /// Builds the ILIKE pattern of a text filter: <c>value*</c> starts with, <c>*value</c> ends with,
    /// <c>*value*</c> contains, and no <c>*</c> equals.
    /// </summary>
    /// <param name="text">The filter as the client sent it.</param>
    /// <returns>The pattern, to use with <see cref="EscapeCharacter"/>.</returns>
    public static string FromWildcards(string text)
    {
        var startsWithWildcard = text.StartsWith('*');
        var literal = startsWithWildcard ? text[1..] : text;
        var endsWithWildcard = literal.EndsWith('*');
        if (endsWithWildcard)
            literal = literal[..^1];

        var pattern = new StringBuilder(literal.Length + 2);
        if (startsWithWildcard)
            pattern.Append('%');
        foreach (var character in literal)
        {
            if (character is '%' or '_' or '\\')
                pattern.Append('\\');
            pattern.Append(character);
        }

        if (endsWithWildcard)
            pattern.Append('%');

        return pattern.ToString();
    }
}
