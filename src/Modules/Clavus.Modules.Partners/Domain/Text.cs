using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Domain;

/// <summary>
/// Normalization and validation of text fields, with the same pt-BR messages across the module
/// </summary>
internal static class Text
{
    /// <summary>
    /// Trim a required field and check its maximum length
    /// </summary>
    /// <param name="value">Informed value</param>
    /// <param name="maxLength">Maximum length</param>
    /// <param name="field">Field name in pt-BR, used in the messages</param>
    /// <returns>The trimmed value</returns>
    /// <exception cref="DomainException">Value is empty or longer than allowed</exception>
    public static string Required(string? value, int maxLength, string field)
    {
        return Optional(value, maxLength, field)
            ?? throw new DomainException($"O campo {field} é obrigatório.");
    }

    /// <summary>
    /// Trim an optional field (blank becomes <see langword="null"/>) and check its maximum length
    /// </summary>
    /// <param name="value">Informed value</param>
    /// <param name="maxLength">Maximum length</param>
    /// <param name="field">Field name in pt-BR, used in the messages</param>
    /// <returns>The trimmed value, or <see langword="null"/> when blank</returns>
    /// <exception cref="DomainException">Value is longer than allowed</exception>
    public static string? Optional(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"O campo {field} deve ter no máximo {maxLength} caracteres.");
        }

        return trimmed;
    }

    /// <summary>
    /// Remove the given formatting characters
    /// </summary>
    /// <param name="value">Informed value</param>
    /// <param name="formatting">Characters to remove</param>
    /// <returns>The value without the formatting characters (empty when <paramref name="value"/> is null)</returns>
    public static string RemoveFormatting(string? value, params char[] formatting)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return string.Concat(value.Where(character => !formatting.Contains(character)));
    }

    /// <summary>
    /// Check whether the value is not empty and has only the digits 0 to 9
    /// </summary>
    /// <param name="value">Value to check</param>
    /// <returns>True if every character is a digit</returns>
    public static bool IsDigits(string value)
    {
        return value.Length > 0 && value.All(char.IsAsciiDigit);
    }
}
