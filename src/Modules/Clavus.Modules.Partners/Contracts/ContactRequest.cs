using System.ComponentModel.DataAnnotations;
using Clavus.Modules.Partners.Domain;

namespace Clavus.Modules.Partners.Contracts;

/// <summary>
/// Contact person of a partner, with own e-mails and phones
/// </summary>
public sealed record ContactRequest
{
    /// <summary>
    /// Identifier of an existing contact of the partner, to keep it on update. Empty for a new contact.
    /// Ignored on create.
    /// </summary>
    public Guid? Identifier { get; init; }

    /// <summary>
    /// Contact name
    /// </summary>
    [Required(ErrorMessage = "O nome do contato é obrigatório.")]
    [StringLength(Contact.NameMaxLength, ErrorMessage = "O nome do contato deve ter no máximo {1} caracteres.")]
    public string? Name { get; init; }

    /// <summary>
    /// Role in the partner (função)
    /// </summary>
    [StringLength(Contact.RoleMaxLength, ErrorMessage = "A função do contato deve ter no máximo {1} caracteres.")]
    public string? Role { get; init; }

    /// <summary>
    /// Contact e-mails. Absent means none.
    /// </summary>
    public IReadOnlyList<EmailRequest>? Emails { get; init; }

    /// <summary>
    /// Contact phones. Absent means none.
    /// </summary>
    public IReadOnlyList<PhoneRequest>? Phones { get; init; }
}
