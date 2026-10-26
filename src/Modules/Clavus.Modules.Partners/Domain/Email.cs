using System.Net.Mail;
using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Domain;

/// <summary>
/// E-mail of a partner or of a contact
/// </summary>
public sealed class Email : Entity
{
    /// <summary>
    /// Maximum length of <see cref="Address"/>
    /// </summary>
    public const int AddressMaxLength = 75;

    /// <summary>
    /// Maximum length of <see cref="Type"/>
    /// </summary>
    public const int TypeMaxLength = 15;

    /// <summary>
    /// Create an e-mail
    /// </summary>
    /// <param name="address">E-mail address</param>
    /// <param name="type">Free text kind, for example "Comercial" or "Financeiro"</param>
    /// <exception cref="DomainException">Empty or invalid address, or empty type, or a field longer than allowed</exception>
    public Email(string? address, string? type)
    {
        var normalizedAddress = Text.Required(address, AddressMaxLength, "e-mail");
        if (!MailAddress.TryCreate(normalizedAddress, out var parsed) || parsed.Address != normalizedAddress)
        {
            throw new DomainException("E-mail inválido.");
        }

        Address = normalizedAddress;
        Type = Text.Required(type, TypeMaxLength, "tipo do e-mail");
    }

    private Email()
    {
        Address = string.Empty;
        Type = string.Empty;
    }

    /// <summary>
    /// E-mail address
    /// </summary>
    public string Address { get; private set; }

    /// <summary>
    /// Free text kind, for example "Comercial" or "Financeiro"
    /// </summary>
    public string Type { get; private set; }
}
