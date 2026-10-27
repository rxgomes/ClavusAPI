using System.Text.Json.Serialization;
using Clavus.Modules.Partners.Domain;

namespace Clavus.Modules.Partners.Contracts;

/// <summary>
/// Complete partner, with address, e-mails, phones and contacts
/// </summary>
/// <param name="Id">Partner id</param>
/// <param name="Identifier">Partner public identifier</param>
/// <param name="Name">Corporate name (razão social) or person name</param>
/// <param name="TradeName">Trade name (nome fantasia)</param>
/// <param name="Document">CPF or CNPJ without formatting</param>
/// <param name="DocumentType"><c>Individual</c> (CPF) or <c>Company</c> (CNPJ)</param>
/// <param name="MunicipalRegistration">Municipal registration (inscrição municipal)</param>
/// <param name="StateRegistration">State registration (inscrição estadual)</param>
/// <param name="Address">Address</param>
/// <param name="IsClient">Indicates whether the partner is a client</param>
/// <param name="IsSupplier">Indicates whether the partner is a supplier</param>
/// <param name="IsActive">Indicates whether the partner is active</param>
/// <param name="CreateIn">Local date and time of the creation</param>
/// <param name="UpdateIn">Local date and time of the last change</param>
/// <param name="Emails">Partner e-mails</param>
/// <param name="Phones">Partner phones</param>
/// <param name="Contacts">Partner contacts</param>
public sealed record PartnerResponse(
    long Id,
    Guid Identifier,
    string Name,
    string TradeName,
    string Document,
    [property: JsonConverter(typeof(JsonStringEnumConverter<DocumentType>))] DocumentType DocumentType,
    string? MunicipalRegistration,
    string? StateRegistration,
    AddressResponse Address,
    bool IsClient,
    bool IsSupplier,
    bool IsActive,
    DateTime CreateIn,
    DateTime UpdateIn,
    IReadOnlyList<EmailResponse> Emails,
    IReadOnlyList<PhoneResponse> Phones,
    IReadOnlyList<ContactResponse> Contacts);

/// <summary>
/// Partner address
/// </summary>
/// <param name="ZipCode">Zip code (CEP), 8 digits</param>
/// <param name="StreetType">Street type (tipo de logradouro)</param>
/// <param name="Street">Street (logradouro)</param>
/// <param name="Number">Number</param>
/// <param name="Neighborhood">Neighborhood (bairro)</param>
/// <param name="City">City</param>
/// <param name="State">State name</param>
/// <param name="StateCode">State code (UF), uppercase</param>
public sealed record AddressResponse(
    string ZipCode,
    string? StreetType,
    string? Street,
    string? Number,
    string? Neighborhood,
    string? City,
    string? State,
    string? StateCode);

/// <summary>
/// E-mail of a partner or of a contact
/// </summary>
/// <param name="Identifier">E-mail public identifier; changes when the owner list is replaced</param>
/// <param name="Address">E-mail address</param>
/// <param name="Type">Free text kind</param>
public sealed record EmailResponse(Guid Identifier, string Address, string Type);

/// <summary>
/// Phone of a partner or of a contact
/// </summary>
/// <param name="Identifier">Phone public identifier; changes when the owner list is replaced</param>
/// <param name="AreaCode">Area code (DDD), 2 digits</param>
/// <param name="Number">Number, 8 or 9 digits</param>
public sealed record PhoneResponse(Guid Identifier, string AreaCode, string Number);

/// <summary>
/// Contact person of a partner
/// </summary>
/// <param name="Identifier">Contact public identifier, kept on update when sent back</param>
/// <param name="Name">Contact name</param>
/// <param name="Role">Role in the partner (função)</param>
/// <param name="Emails">Contact e-mails</param>
/// <param name="Phones">Contact phones</param>
public sealed record ContactResponse(
    Guid Identifier,
    string Name,
    string? Role,
    IReadOnlyList<EmailResponse> Emails,
    IReadOnlyList<PhoneResponse> Phones);

/// <summary>
/// Partner summary, returned by the paged search
/// </summary>
/// <param name="Id">Partner id</param>
/// <param name="Identifier">Partner public identifier</param>
/// <param name="Name">Corporate name (razão social) or person name</param>
/// <param name="TradeName">Trade name (nome fantasia)</param>
/// <param name="Document">CPF or CNPJ without formatting</param>
/// <param name="DocumentType"><c>Individual</c> (CPF) or <c>Company</c> (CNPJ)</param>
/// <param name="IsClient">Indicates whether the partner is a client</param>
/// <param name="IsSupplier">Indicates whether the partner is a supplier</param>
/// <param name="IsActive">Indicates whether the partner is active</param>
/// <param name="City">Address city</param>
/// <param name="StateCode">Address state code (UF)</param>
public sealed record PartnerSummaryResponse(
    long Id,
    Guid Identifier,
    string Name,
    string TradeName,
    string Document,
    [property: JsonConverter(typeof(JsonStringEnumConverter<DocumentType>))] DocumentType DocumentType,
    bool IsClient,
    bool IsSupplier,
    bool IsActive,
    string? City,
    string? StateCode);
