using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Domain;

/// <summary>
/// Business partner: client and/or supplier, individual (CPF) or company (CNPJ)
/// </summary>
public sealed class Partner : Entity
{
    /// <summary>
    /// Maximum length of <see cref="Name"/> and <see cref="TradeName"/>
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Maximum length of <see cref="MunicipalRegistration"/> and <see cref="StateRegistration"/>
    /// </summary>
    public const int RegistrationMaxLength = 15;

    private readonly List<Contact> _contacts = [];

    private readonly List<Email> _emails = [];

    private readonly List<Phone> _phones = [];

    /// <summary>
    /// Create a partner, neither client nor supplier
    /// </summary>
    /// <param name="name">Corporate name (razão social) or person name</param>
    /// <param name="tradeName">Trade name (nome fantasia)</param>
    /// <param name="document">CPF or CNPJ</param>
    /// <param name="address">Address</param>
    /// <param name="municipalRegistration">Municipal registration (inscrição municipal), optional</param>
    /// <param name="stateRegistration">State registration (inscrição estadual), optional</param>
    /// <exception cref="DomainException">Empty name or trade name, or a field longer than allowed</exception>
    public Partner(
        string? name,
        string? tradeName,
        Document document,
        Address address,
        string? municipalRegistration = null,
        string? stateRegistration = null)
    {
        Name = string.Empty;
        TradeName = string.Empty;
        Rename(name, tradeName);
        ChangeRegistrations(municipalRegistration, stateRegistration);
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    private Partner()
    {
        Name = string.Empty;
        TradeName = string.Empty;
        Document = null!;
        Address = null!;
    }

    /// <summary>
    /// Corporate name (razão social) or person name
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Trade name (nome fantasia)
    /// </summary>
    public string TradeName { get; private set; }

    /// <summary>
    /// CPF or CNPJ, unique among partners
    /// </summary>
    public Document Document { get; private set; }

    /// <summary>
    /// Municipal registration (inscrição municipal)
    /// </summary>
    public string? MunicipalRegistration { get; private set; }

    /// <summary>
    /// State registration (inscrição estadual)
    /// </summary>
    public string? StateRegistration { get; private set; }

    /// <summary>
    /// Address
    /// </summary>
    public Address Address { get; private set; }

    /// <summary>
    /// Indicates whether the partner is a client
    /// </summary>
    public bool IsClient { get; set; }

    /// <summary>
    /// Indicates whether the partner is a supplier
    /// </summary>
    public bool IsSupplier { get; set; }

    /// <summary>
    /// Partner contacts
    /// </summary>
    public IReadOnlyCollection<Contact> Contacts => _contacts;

    /// <summary>
    /// Partner e-mails (the contacts have their own)
    /// </summary>
    public IReadOnlyCollection<Email> Emails => _emails;

    /// <summary>
    /// Partner phones (the contacts have their own)
    /// </summary>
    public IReadOnlyCollection<Phone> Phones => _phones;

    /// <summary>
    /// Change the corporate name and the trade name
    /// </summary>
    /// <param name="name">Corporate name (razão social) or person name</param>
    /// <param name="tradeName">Trade name (nome fantasia)</param>
    /// <exception cref="DomainException">Empty value or longer than allowed</exception>
    public void Rename(string? name, string? tradeName)
    {
        var normalizedName = Text.Required(name, NameMaxLength, "razão social");
        var normalizedTradeName = Text.Required(tradeName, NameMaxLength, "nome fantasia");

        Name = normalizedName;
        TradeName = normalizedTradeName;
    }

    /// <summary>
    /// Change the document (CPF or CNPJ)
    /// </summary>
    /// <param name="document">New document</param>
    public void ChangeDocument(Document document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }

    /// <summary>
    /// Change the municipal and state registrations
    /// </summary>
    /// <param name="municipalRegistration">Municipal registration (inscrição municipal), optional</param>
    /// <param name="stateRegistration">State registration (inscrição estadual), optional</param>
    /// <exception cref="DomainException">Value longer than allowed</exception>
    public void ChangeRegistrations(string? municipalRegistration, string? stateRegistration)
    {
        var normalizedMunicipal = Text.Optional(municipalRegistration, RegistrationMaxLength, "inscrição municipal");
        var normalizedState = Text.Optional(stateRegistration, RegistrationMaxLength, "inscrição estadual");

        MunicipalRegistration = normalizedMunicipal;
        StateRegistration = normalizedState;
    }

    /// <summary>
    /// Replace the address
    /// </summary>
    /// <param name="address">New address</param>
    public void ChangeAddress(Address address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    /// <summary>
    /// Add a contact to the partner
    /// </summary>
    /// <param name="contact">Contact to add</param>
    public void AddContact(Contact contact)
    {
        ArgumentNullException.ThrowIfNull(contact);
        _contacts.Add(contact);
    }

    /// <summary>
    /// Remove a contact from the partner; it is deleted, with its e-mails and phones, when the partner is saved
    /// </summary>
    /// <param name="contact">Contact to remove</param>
    /// <returns>True if the contact belonged to the partner</returns>
    public bool RemoveContact(Contact contact)
    {
        return _contacts.Remove(contact);
    }

    /// <summary>
    /// Add an e-mail to the partner
    /// </summary>
    /// <param name="email">E-mail to add</param>
    public void AddEmail(Email email)
    {
        ArgumentNullException.ThrowIfNull(email);
        _emails.Add(email);
    }

    /// <summary>
    /// Remove an e-mail from the partner; it is deleted when the partner is saved
    /// </summary>
    /// <param name="email">E-mail to remove</param>
    /// <returns>True if the e-mail belonged to the partner</returns>
    public bool RemoveEmail(Email email)
    {
        return _emails.Remove(email);
    }

    /// <summary>
    /// Add a phone to the partner
    /// </summary>
    /// <param name="phone">Phone to add</param>
    public void AddPhone(Phone phone)
    {
        ArgumentNullException.ThrowIfNull(phone);
        _phones.Add(phone);
    }

    /// <summary>
    /// Remove a phone from the partner; it is deleted when the partner is saved
    /// </summary>
    /// <param name="phone">Phone to remove</param>
    /// <returns>True if the phone belonged to the partner</returns>
    public bool RemovePhone(Phone phone)
    {
        return _phones.Remove(phone);
    }
}
