using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Domain;

/// <summary>
/// Phone of a partner or of a contact, kept without formatting
/// </summary>
public sealed class Phone : Entity
{
    /// <summary>
    /// Length of <see cref="AreaCode"/>
    /// </summary>
    public const int AreaCodeLength = 2;

    /// <summary>
    /// Maximum length of <see cref="Number"/> (mobile); landlines have 8 digits
    /// </summary>
    public const int NumberMaxLength = 9;

    private const int NumberMinLength = 8;

    private const int MinAreaCode = 11;

    private static readonly char[] Formatting = ['(', ')', '-', '.', ' '];

    /// <summary>
    /// Create a phone
    /// </summary>
    /// <param name="areaCode">Area code (DDD), formatted or not, between 11 and 99</param>
    /// <param name="number">Number, formatted or not, with 8 or 9 digits</param>
    /// <exception cref="DomainException">Empty or invalid area code or number</exception>
    public Phone(string? areaCode, string? number)
    {
        var normalizedAreaCode = Text.RemoveFormatting(areaCode, Formatting);
        if (normalizedAreaCode.Length == 0)
        {
            throw new DomainException("O campo DDD é obrigatório.");
        }

        if (normalizedAreaCode.Length != AreaCodeLength || !Text.IsDigits(normalizedAreaCode) || int.Parse(normalizedAreaCode) < MinAreaCode)
        {
            throw new DomainException("DDD inválido: informe 2 dígitos entre 11 e 99.");
        }

        var normalizedNumber = Text.RemoveFormatting(number, Formatting);
        if (normalizedNumber.Length == 0)
        {
            throw new DomainException("O campo número do telefone é obrigatório.");
        }

        if (normalizedNumber.Length is < NumberMinLength or > NumberMaxLength || !Text.IsDigits(normalizedNumber))
        {
            throw new DomainException("Número de telefone inválido: informe 8 ou 9 dígitos.");
        }

        AreaCode = normalizedAreaCode;
        Number = normalizedNumber;
    }

    private Phone()
    {
        AreaCode = string.Empty;
        Number = string.Empty;
    }

    /// <summary>
    /// Area code (DDD), 2 digits
    /// </summary>
    public string AreaCode { get; private set; }

    /// <summary>
    /// Number, 8 (landline) or 9 (mobile) digits
    /// </summary>
    public string Number { get; private set; }
}
