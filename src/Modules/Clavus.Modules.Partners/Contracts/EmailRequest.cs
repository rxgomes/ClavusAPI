using System.ComponentModel.DataAnnotations;
using Clavus.Modules.Partners.Domain;

namespace Clavus.Modules.Partners.Contracts;

/// <summary>
/// E-mail of a partner or of a contact
/// </summary>
public sealed record EmailRequest
{
    /// <summary>
    /// E-mail address
    /// </summary>
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [StringLength(Email.AddressMaxLength, ErrorMessage = "O e-mail deve ter no máximo {1} caracteres.")]
    public string? Address { get; init; }

    /// <summary>
    /// Free text kind, for example <c>Comercial</c> or <c>Financeiro</c>
    /// </summary>
    [Required(ErrorMessage = "O tipo do e-mail é obrigatório.")]
    [StringLength(Email.TypeMaxLength, ErrorMessage = "O tipo do e-mail deve ter no máximo {1} caracteres.")]
    public string? Type { get; init; }
}
