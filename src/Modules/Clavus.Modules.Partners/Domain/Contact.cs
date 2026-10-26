using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Domain;

/// <summary>
/// Contact person of a partner, with own e-mails and phones
/// </summary>
public sealed class Contact : Entity
{
    /// <summary>
    /// Maximum length of <see cref="Name"/>
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Maximum length of <see cref="Role"/>
    /// </summary>
    public const int RoleMaxLength = 30;

    private readonly List<Email> _emails = [];

    private readonly List<Phone> _phones = [];

    /// <summary>
    /// Create a contact
    /// </summary>
    /// <param name="name">Contact name</param>
    /// <param name="role">Role in the partner (função), optional</param>
    /// <exception cref="DomainException">Empty name or a field longer than allowed</exception>
    public Contact(string? name, string? role = null)
    {
        Name = string.Empty;
        Update(name, role);
    }

    private Contact()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Contact name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Role in the partner (função)
    /// </summary>
    public string? Role { get; private set; }

    /// <summary>
    /// Contact e-mails
    /// </summary>
    public IReadOnlyCollection<Email> Emails => _emails;

    /// <summary>
    /// Contact phones
    /// </summary>
    public IReadOnlyCollection<Phone> Phones => _phones;

    /// <summary>
    /// Change the contact name and role
    /// </summary>
    /// <param name="name">Contact name</param>
    /// <param name="role">Role in the partner (função), optional</param>
    /// <exception cref="DomainException">Empty name or a field longer than allowed</exception>
    public void Update(string? name, string? role)
    {
        var normalizedName = Text.Required(name, NameMaxLength, "nome do contato");
        var normalizedRole = Text.Optional(role, RoleMaxLength, "função");

        Name = normalizedName;
        Role = normalizedRole;
    }

    /// <summary>
    /// Add an e-mail to the contact
    /// </summary>
    /// <param name="email">E-mail to add</param>
    public void AddEmail(Email email)
    {
        ArgumentNullException.ThrowIfNull(email);
        _emails.Add(email);
    }

    /// <summary>
    /// Remove an e-mail from the contact; it is deleted when the partner is saved
    /// </summary>
    /// <param name="email">E-mail to remove</param>
    /// <returns>True if the e-mail belonged to the contact</returns>
    public bool RemoveEmail(Email email)
    {
        return _emails.Remove(email);
    }

    /// <summary>
    /// Add a phone to the contact
    /// </summary>
    /// <param name="phone">Phone to add</param>
    public void AddPhone(Phone phone)
    {
        ArgumentNullException.ThrowIfNull(phone);
        _phones.Add(phone);
    }

    /// <summary>
    /// Remove a phone from the contact; it is deleted when the partner is saved
    /// </summary>
    /// <param name="phone">Phone to remove</param>
    /// <returns>True if the phone belonged to the contact</returns>
    public bool RemovePhone(Phone phone)
    {
        return _phones.Remove(phone);
    }
}
