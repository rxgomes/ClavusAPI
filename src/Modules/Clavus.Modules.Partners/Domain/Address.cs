using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Domain;

/// <summary>
/// Partner address. Only the zip code (CEP) is required; it is kept without formatting.
/// </summary>
public sealed record Address
{
    /// <summary>
    /// Maximum length of <see cref="StreetType"/>
    /// </summary>
    public const int StreetTypeMaxLength = 5;

    /// <summary>
    /// Maximum length of <see cref="Street"/>, <see cref="Neighborhood"/> and <see cref="City"/>
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Maximum length of <see cref="Number"/>
    /// </summary>
    public const int NumberMaxLength = 10;

    /// <summary>
    /// Maximum length of <see cref="State"/>
    /// </summary>
    public const int StateMaxLength = 35;

    /// <summary>
    /// Length of <see cref="ZipCode"/>
    /// </summary>
    public const int ZipCodeLength = 8;

    /// <summary>
    /// Length of <see cref="StateCode"/>
    /// </summary>
    public const int StateCodeLength = 2;

    private static readonly char[] ZipCodeFormatting = ['-', '.', ' '];

    private static readonly HashSet<string> StateCodes =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    ];

    /// <summary>
    /// Create an address. Optional fields are trimmed and blank values become <see langword="null"/>.
    /// </summary>
    /// <param name="zipCode">CEP, formatted or not (8 digits)</param>
    /// <param name="streetType">Street type (tipo de logradouro), for example "Rua"</param>
    /// <param name="street">Street (logradouro)</param>
    /// <param name="number">Number</param>
    /// <param name="neighborhood">Neighborhood (bairro)</param>
    /// <param name="city">City</param>
    /// <param name="state">State name</param>
    /// <param name="stateCode">State code (UF), case insensitive</param>
    /// <exception cref="DomainException">Invalid zip code or state code, or a field longer than allowed</exception>
    public Address(
        string? zipCode,
        string? streetType = null,
        string? street = null,
        string? number = null,
        string? neighborhood = null,
        string? city = null,
        string? state = null,
        string? stateCode = null)
    {
        var normalizedZipCode = Text.RemoveFormatting(zipCode, ZipCodeFormatting);
        if (normalizedZipCode.Length == 0)
        {
            throw new DomainException("O campo CEP é obrigatório.");
        }

        if (normalizedZipCode.Length != ZipCodeLength || !Text.IsDigits(normalizedZipCode))
        {
            throw new DomainException("CEP inválido: informe 8 dígitos.");
        }

        var normalizedStateCode = Text.Optional(stateCode, StateCodeLength, "UF")?.ToUpperInvariant();
        if (normalizedStateCode is not null && !StateCodes.Contains(normalizedStateCode))
        {
            throw new DomainException("UF inválida.");
        }

        ZipCode = normalizedZipCode;
        StreetType = Text.Optional(streetType, StreetTypeMaxLength, "tipo de logradouro");
        Street = Text.Optional(street, NameMaxLength, "logradouro");
        Number = Text.Optional(number, NumberMaxLength, "número");
        Neighborhood = Text.Optional(neighborhood, NameMaxLength, "bairro");
        City = Text.Optional(city, NameMaxLength, "cidade");
        State = Text.Optional(state, StateMaxLength, "estado");
        StateCode = normalizedStateCode;
    }

    private Address()
    {
        ZipCode = string.Empty;
    }

    /// <summary>
    /// Zip code (CEP), 8 digits without formatting
    /// </summary>
    public string ZipCode { get; private set; }

    /// <summary>
    /// Street type (tipo de logradouro)
    /// </summary>
    public string? StreetType { get; private set; }

    /// <summary>
    /// Street (logradouro)
    /// </summary>
    public string? Street { get; private set; }

    /// <summary>
    /// Number
    /// </summary>
    public string? Number { get; private set; }

    /// <summary>
    /// Neighborhood (bairro)
    /// </summary>
    public string? Neighborhood { get; private set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; private set; }

    /// <summary>
    /// State name
    /// </summary>
    public string? State { get; private set; }

    /// <summary>
    /// State code (UF), uppercase
    /// </summary>
    public string? StateCode { get; private set; }
}
