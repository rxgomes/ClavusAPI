using System.ComponentModel.DataAnnotations;
using Clavus.Modules.Partners.Domain;

namespace Clavus.Modules.Partners.Contracts;

/// <summary>
/// Partner data sent on create and on update. Presence and length are checked here; the format rules
/// (CPF/CNPJ, CEP, state code, phone, e-mail) are checked by the domain.
/// </summary>
public abstract record PartnerRequest : IValidatableObject
{
    private const int FormattedDocumentMaxLength = 20;

    /// <summary>
    /// Corporate name (razão social) or person name
    /// </summary>
    [Required(ErrorMessage = "A razão social é obrigatória.")]
    [StringLength(Partner.NameMaxLength, ErrorMessage = "A razão social deve ter no máximo {1} caracteres.")]
    public string? Name { get; init; }

    /// <summary>
    /// Trade name (nome fantasia)
    /// </summary>
    [Required(ErrorMessage = "O nome fantasia é obrigatório.")]
    [StringLength(Partner.NameMaxLength, ErrorMessage = "O nome fantasia deve ter no máximo {1} caracteres.")]
    public string? TradeName { get; init; }

    /// <summary>
    /// CPF or CNPJ (numeric or alphanumeric), formatted or not
    /// </summary>
    [Required(ErrorMessage = "O documento é obrigatório.")]
    [StringLength(FormattedDocumentMaxLength, ErrorMessage = "O documento deve ter no máximo {1} caracteres.")]
    public string? Document { get; init; }

    /// <summary>
    /// Municipal registration (inscrição municipal)
    /// </summary>
    [StringLength(Partner.RegistrationMaxLength, ErrorMessage = "A inscrição municipal deve ter no máximo {1} caracteres.")]
    public string? MunicipalRegistration { get; init; }

    /// <summary>
    /// State registration (inscrição estadual)
    /// </summary>
    [StringLength(Partner.RegistrationMaxLength, ErrorMessage = "A inscrição estadual deve ter no máximo {1} caracteres.")]
    public string? StateRegistration { get; init; }

    /// <summary>
    /// Address
    /// </summary>
    [Required(ErrorMessage = "O endereço é obrigatório.")]
    public AddressRequest? Address { get; init; }

    /// <summary>
    /// Indicates whether the partner is a client. <see cref="IsClient"/> or <see cref="IsSupplier"/> must be true.
    /// </summary>
    public bool IsClient { get; init; }

    /// <summary>
    /// Indicates whether the partner is a supplier. <see cref="IsClient"/> or <see cref="IsSupplier"/> must be true.
    /// </summary>
    public bool IsSupplier { get; init; }

    /// <summary>
    /// Partner e-mails. Absent means none; on update, replaces the current list.
    /// </summary>
    public IReadOnlyList<EmailRequest>? Emails { get; init; }

    /// <summary>
    /// Partner phones. Absent means none; on update, replaces the current list.
    /// </summary>
    public IReadOnlyList<PhoneRequest>? Phones { get; init; }

    /// <summary>
    /// Partner contacts. Absent means none; on update, replaces the current list (contacts sent with
    /// <see cref="ContactRequest.Identifier"/> are kept and updated).
    /// </summary>
    public IReadOnlyList<ContactRequest>? Contacts { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsClient && !IsSupplier)
        {
            yield return new ValidationResult(
                "O parceiro deve ser cliente, fornecedor ou ambos.",
                [nameof(IsClient), nameof(IsSupplier)]);
        }
    }
}

/// <summary>
/// Data to create a partner
/// </summary>
public sealed record CreatePartnerRequest : PartnerRequest;

/// <summary>
/// Data to update a partner: replaces all its data and lists
/// </summary>
public sealed record UpdatePartnerRequest : PartnerRequest
{
    /// <summary>
    /// Indicates whether the partner is active. Sending <see langword="true"/> reactivates an inactive partner.
    /// </summary>
    [Required(ErrorMessage = "A situação (isActive) é obrigatória.")]
    public bool? IsActive { get; init; }
}
