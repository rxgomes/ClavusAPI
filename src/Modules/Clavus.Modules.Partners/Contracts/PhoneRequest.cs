using System.ComponentModel.DataAnnotations;

namespace Clavus.Modules.Partners.Contracts;

/// <summary>
/// Phone of a partner or of a contact. The format rules (area code between 11 and 99, 8 or 9 digits)
/// are checked by the domain.
/// </summary>
public sealed record PhoneRequest
{
    private const int FormattedAreaCodeMaxLength = 4;

    private const int FormattedNumberMaxLength = 15;

    /// <summary>
    /// Area code (DDD), formatted or not, for example <c>(11)</c>
    /// </summary>
    [Required(ErrorMessage = "O DDD é obrigatório.")]
    [StringLength(FormattedAreaCodeMaxLength, ErrorMessage = "O DDD deve ter no máximo {1} caracteres.")]
    public string? AreaCode { get; init; }

    /// <summary>
    /// Number, formatted or not, for example <c>98765-4321</c>
    /// </summary>
    [Required(ErrorMessage = "O número do telefone é obrigatório.")]
    [StringLength(FormattedNumberMaxLength, ErrorMessage = "O número do telefone deve ter no máximo {1} caracteres.")]
    public string? Number { get; init; }
}
