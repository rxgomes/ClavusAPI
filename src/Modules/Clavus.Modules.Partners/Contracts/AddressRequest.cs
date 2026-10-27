using System.ComponentModel.DataAnnotations;
using Clavus.Modules.Partners.Domain;

namespace Clavus.Modules.Partners.Contracts;

/// <summary>
/// Partner address. Only the zip code (CEP) is required; the format rules (8 digits, valid state code)
/// are checked by the domain.
/// </summary>
public sealed record AddressRequest
{
    private const int FormattedZipCodeMaxLength = 10;

    /// <summary>
    /// Zip code (CEP), formatted or not, for example <c>01310-100</c>
    /// </summary>
    [Required(ErrorMessage = "O CEP é obrigatório.")]
    [StringLength(FormattedZipCodeMaxLength, ErrorMessage = "O CEP deve ter no máximo {1} caracteres.")]
    public string? ZipCode { get; init; }

    /// <summary>
    /// Street type (tipo de logradouro), for example <c>Rua</c>
    /// </summary>
    [StringLength(Address.StreetTypeMaxLength, ErrorMessage = "O tipo de logradouro deve ter no máximo {1} caracteres.")]
    public string? StreetType { get; init; }

    /// <summary>
    /// Street (logradouro)
    /// </summary>
    [StringLength(Address.NameMaxLength, ErrorMessage = "O logradouro deve ter no máximo {1} caracteres.")]
    public string? Street { get; init; }

    /// <summary>
    /// Number
    /// </summary>
    [StringLength(Address.NumberMaxLength, ErrorMessage = "O número deve ter no máximo {1} caracteres.")]
    public string? Number { get; init; }

    /// <summary>
    /// Neighborhood (bairro)
    /// </summary>
    [StringLength(Address.NameMaxLength, ErrorMessage = "O bairro deve ter no máximo {1} caracteres.")]
    public string? Neighborhood { get; init; }

    /// <summary>
    /// City
    /// </summary>
    [StringLength(Address.NameMaxLength, ErrorMessage = "A cidade deve ter no máximo {1} caracteres.")]
    public string? City { get; init; }

    /// <summary>
    /// State name
    /// </summary>
    [StringLength(Address.StateMaxLength, ErrorMessage = "O estado deve ter no máximo {1} caracteres.")]
    public string? State { get; init; }

    /// <summary>
    /// State code (UF), case insensitive, for example <c>SP</c>
    /// </summary>
    [StringLength(Address.StateCodeLength, ErrorMessage = "A UF deve ter no máximo {1} caracteres.")]
    public string? StateCode { get; init; }
}
