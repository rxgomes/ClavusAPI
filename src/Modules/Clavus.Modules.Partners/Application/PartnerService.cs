using Clavus.Modules.Partners.Contracts;
using Clavus.Modules.Partners.Domain;
using Clavus.Modules.Partners.Persistence;
using Clavus.Shared.Abstractions.Domain;
using Clavus.Shared.Abstractions.Pagination;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Clavus.Modules.Partners.Application;

/// <summary>
/// Partner use cases over <see cref="PartnersDbContext"/>
/// </summary>
internal sealed class PartnerService(PartnersDbContext context) : IPartnerService
{
    private const string DocumentIndexName = "ix_partner_document";

    public async Task<PartnerResult> CreateAsync(CreatePartnerRequest request, CancellationToken cancellationToken)
    {
        var partner = new Partner(
            request.Name,
            request.TradeName,
            new Document(request.Document),
            ToAddress(request.Address!),
            request.MunicipalRegistration,
            request.StateRegistration)
        {
            IsClient = request.IsClient,
            IsSupplier = request.IsSupplier,
        };

        foreach (var email in ToEmails(request.Emails))
        {
            partner.AddEmail(email);
        }

        foreach (var phone in ToPhones(request.Phones))
        {
            partner.AddPhone(phone);
        }

        foreach (var contactRequest in NotNull(request.Contacts))
        {
            partner.AddContact(ToContact(contactRequest));
        }

        if (await DocumentExistsAsync(partner.Document, null, cancellationToken))
        {
            return PartnerResult.DuplicateDocument;
        }

        context.Set<Partner>().Add(partner);
        if (!await TrySaveAsync(cancellationToken))
        {
            return PartnerResult.DuplicateDocument;
        }

        return PartnerResult.Success(ToResponse(partner));
    }

    public async Task<PagedResult<PartnerSummaryResponse>> SearchAsync(PartnerSearchQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? PartnerSearchQuery.DefaultPage;
        var pageSize = query.PageSize ?? PartnerSearchQuery.DefaultPageSize;
        var isActive = query.IsActive ?? true;

        var partners = Filter(context.Set<Partner>().AsNoTracking(), query)
            .Where(partner => partner.IsActive == isActive);

        var totalCount = await partners.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);

        var rows = await partners
            .OrderBy(partner => partner.TradeName)
            .ThenBy(partner => partner.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(partner => new
            {
                partner.Id,
                partner.Identifier,
                partner.Name,
                partner.TradeName,
                partner.Document,
                partner.IsClient,
                partner.IsSupplier,
                partner.IsActive,
                partner.Address.City,
                partner.Address.StateCode,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new PartnerSummaryResponse(
                row.Id,
                row.Identifier,
                row.Name,
                row.TradeName,
                row.Document.Value,
                row.Document.Type,
                row.IsClient,
                row.IsSupplier,
                row.IsActive,
                row.City,
                row.StateCode))
            .ToList();

        return new PagedResult<PartnerSummaryResponse>(items, page, pageSize, totalCount);
    }

    public async Task<PartnerResponse?> GetDetailsAsync(PartnerDetailsQuery query, CancellationToken cancellationToken)
    {
        var partner = await Filter(WithLists(context.Set<Partner>().AsNoTracking()), query)
            .FirstOrDefaultAsync(cancellationToken);

        return partner is null ? null : ToResponse(partner);
    }

    public async Task<PartnerResult> UpdateAsync(Guid identifier, UpdatePartnerRequest request, CancellationToken cancellationToken)
    {
        var partner = await WithLists(context.Set<Partner>())
            .FirstOrDefaultAsync(p => p.Identifier == identifier, cancellationToken);
        if (partner is null)
        {
            return PartnerResult.NotFound;
        }

        // Value objects and new items are built first, so a broken rule fails before the aggregate changes.
        var document = new Document(request.Document);
        var address = ToAddress(request.Address!);
        var emails = ToEmails(request.Emails);
        var phones = ToPhones(request.Phones);
        var contactChanges = MatchContacts(partner, NotNull(request.Contacts));

        if (await DocumentExistsAsync(document, partner.Id, cancellationToken))
        {
            return PartnerResult.DuplicateDocument;
        }

        partner.Rename(request.Name, request.TradeName);
        partner.ChangeDocument(document);
        partner.ChangeRegistrations(request.MunicipalRegistration, request.StateRegistration);
        partner.ChangeAddress(address);
        partner.IsClient = request.IsClient;
        partner.IsSupplier = request.IsSupplier;
        partner.IsActive = request.IsActive!.Value;

        ReplaceEmails(partner.Emails, partner.RemoveEmail, partner.AddEmail, emails);
        ReplacePhones(partner.Phones, partner.RemovePhone, partner.AddPhone, phones);

        var keptContacts = contactChanges
            .Where(change => change.Existing is not null)
            .Select(change => change.Existing!)
            .ToHashSet();
        foreach (var removed in partner.Contacts.Where(contact => !keptContacts.Contains(contact)).ToList())
        {
            partner.RemoveContact(removed);
        }

        foreach (var change in contactChanges)
        {
            if (change.Existing is null)
            {
                partner.AddContact(ToContact(change.Request));
                continue;
            }

            change.Existing.Update(change.Request.Name, change.Request.Role);
            ReplaceEmails(change.Existing.Emails, change.Existing.RemoveEmail, change.Existing.AddEmail, ToEmails(change.Request.Emails));
            ReplacePhones(change.Existing.Phones, change.Existing.RemovePhone, change.Existing.AddPhone, ToPhones(change.Request.Phones));
        }

        if (!await TrySaveAsync(cancellationToken))
        {
            return PartnerResult.DuplicateDocument;
        }

        return PartnerResult.Success(ToResponse(partner));
    }

    public async Task<bool> DeactivateAsync(Guid identifier, CancellationToken cancellationToken)
    {
        var partner = await context.Set<Partner>()
            .FirstOrDefaultAsync(p => p.Identifier == identifier, cancellationToken);
        if (partner is null)
        {
            return false;
        }

        if (partner.IsActive)
        {
            partner.IsActive = false;
            await context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private static IQueryable<Partner> Filter(IQueryable<Partner> partners, PartnerFilter filter)
    {
        partners = filter.SearchesClients
            ? partners.Where(partner => partner.IsClient)
            : partners.Where(partner => partner.IsSupplier);

        if (filter.Id is { } id)
        {
            partners = partners.Where(partner => partner.Id == id);
        }

        if (filter.Identifier is { } identifier)
        {
            partners = partners.Where(partner => partner.Identifier == identifier);
        }

        return partners;
    }

    private static IQueryable<Partner> WithLists(IQueryable<Partner> partners)
    {
        return partners
            .Include(partner => partner.Emails)
            .Include(partner => partner.Phones)
            .Include(partner => partner.Contacts).ThenInclude(contact => contact.Emails)
            .Include(partner => partner.Contacts).ThenInclude(contact => contact.Phones)
            .AsSplitQuery();
    }

    private async Task<bool> DocumentExistsAsync(Document document, long? exceptPartnerId, CancellationToken cancellationToken)
    {
        return await context.Set<Partner>()
            .AnyAsync(partner => partner.Document == document && partner.Id != exceptPartnerId, cancellationToken);
    }

    /// <summary>
    /// Save the changes; false when another request saved the same document after the duplicate check
    /// </summary>
    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DocumentIndexName,
        })
        {
            return false;
        }
    }

    private static List<ContactChange> MatchContacts(Partner partner, IEnumerable<ContactRequest> requests)
    {
        var current = partner.Contacts.ToDictionary(contact => contact.Identifier);
        var matched = new HashSet<Guid>();
        var changes = new List<ContactChange>();

        foreach (var request in requests)
        {
            if (request.Identifier is not { } identifier)
            {
                // New contact: built now to validate it before the aggregate changes.
                ToContact(request);
                changes.Add(new ContactChange(null, request));
                continue;
            }

            if (!current.TryGetValue(identifier, out var existing))
            {
                throw new DomainException("Contato não encontrado neste parceiro.");
            }

            if (!matched.Add(identifier))
            {
                throw new DomainException("Contato informado mais de uma vez.");
            }

            // Validates name, role, e-mails and phones before the aggregate changes.
            ToContact(request);
            changes.Add(new ContactChange(existing, request));
        }

        return changes;
    }

    private static void ReplaceEmails(IReadOnlyCollection<Email> current, Func<Email, bool> remove, Action<Email> add, List<Email> emails)
    {
        foreach (var email in current.ToList())
        {
            remove(email);
        }

        emails.ForEach(add);
    }

    private static void ReplacePhones(IReadOnlyCollection<Phone> current, Func<Phone, bool> remove, Action<Phone> add, List<Phone> phones)
    {
        foreach (var phone in current.ToList())
        {
            remove(phone);
        }

        phones.ForEach(add);
    }

    private static IEnumerable<T> NotNull<T>(IEnumerable<T?>? items)
        where T : class
    {
        return items?.OfType<T>() ?? [];
    }

    private static Address ToAddress(AddressRequest request)
    {
        return new Address(
            request.ZipCode,
            request.StreetType,
            request.Street,
            request.Number,
            request.Neighborhood,
            request.City,
            request.State,
            request.StateCode);
    }

    private static List<Email> ToEmails(IEnumerable<EmailRequest?>? requests)
    {
        return NotNull(requests).Select(request => new Email(request.Address, request.Type)).ToList();
    }

    private static List<Phone> ToPhones(IEnumerable<PhoneRequest?>? requests)
    {
        return NotNull(requests).Select(request => new Phone(request.AreaCode, request.Number)).ToList();
    }

    private static Contact ToContact(ContactRequest request)
    {
        var contact = new Contact(request.Name, request.Role);

        foreach (var email in ToEmails(request.Emails))
        {
            contact.AddEmail(email);
        }

        foreach (var phone in ToPhones(request.Phones))
        {
            contact.AddPhone(phone);
        }

        return contact;
    }

    private static PartnerResponse ToResponse(Partner partner)
    {
        return new PartnerResponse(
            partner.Id,
            partner.Identifier,
            partner.Name,
            partner.TradeName,
            partner.Document.Value,
            partner.Document.Type,
            partner.MunicipalRegistration,
            partner.StateRegistration,
            new AddressResponse(
                partner.Address.ZipCode,
                partner.Address.StreetType,
                partner.Address.Street,
                partner.Address.Number,
                partner.Address.Neighborhood,
                partner.Address.City,
                partner.Address.State,
                partner.Address.StateCode),
            partner.IsClient,
            partner.IsSupplier,
            partner.IsActive,
            partner.CreateIn,
            partner.UpdateIn,
            ToResponses(partner.Emails),
            ToResponses(partner.Phones),
            partner.Contacts
                .OrderBy(contact => contact.Id)
                .Select(contact => new ContactResponse(
                    contact.Identifier,
                    contact.Name,
                    contact.Role,
                    ToResponses(contact.Emails),
                    ToResponses(contact.Phones)))
                .ToList());
    }

    private static List<EmailResponse> ToResponses(IEnumerable<Email> emails)
    {
        return emails
            .OrderBy(email => email.Id)
            .Select(email => new EmailResponse(email.Identifier, email.Address, email.Type))
            .ToList();
    }

    private static List<PhoneResponse> ToResponses(IEnumerable<Phone> phones)
    {
        return phones
            .OrderBy(phone => phone.Id)
            .Select(phone => new PhoneResponse(phone.Identifier, phone.AreaCode, phone.Number))
            .ToList();
    }

    /// <summary>
    /// Contact sent on update: an existing contact of the partner to keep, or <see langword="null"/> for a new one
    /// </summary>
    private sealed record ContactChange(Contact? Existing, ContactRequest Request);
}
