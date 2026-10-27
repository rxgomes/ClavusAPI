using Clavus.Modules.Partners.Contracts;
using Clavus.Shared.Abstractions.Domain;
using Clavus.Shared.Abstractions.Pagination;

namespace Clavus.Modules.Partners.Application;

/// <summary>
/// Partner registration use cases: create, search, details, update and deactivation
/// </summary>
/// <author>Ricardo Gomes</author>
/// <created>2026-10-26</created>
/// <modified>2026-10-26</modified>
public interface IPartnerService
{
    /// <summary>
    /// Create a partner with its address, e-mails, phones and contacts
    /// </summary>
    /// <param name="request">Partner data, already validated for presence and length</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created partner, or <see cref="PartnerResultStatus.DuplicateDocument"/> when the document is already registered</returns>
    /// <exception cref="DomainException">A value breaks a partner rule (document, zip code, state code, phone, e-mail)</exception>
    /// <author>Ricardo Gomes</author>
    /// <created>2026-10-26</created>
    /// <modified>2026-10-26</modified>
    Task<PartnerResult> CreateAsync(CreatePartnerRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Search partners of one type (client or supplier) and status, optionally by id or identifier, ordered by
    /// trade name and id
    /// </summary>
    /// <param name="query">Search filters, already validated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested page and the total number of matching partners</returns>
    /// <author>Ricardo Gomes</author>
    /// <created>2026-10-26</created>
    /// <modified>2026-10-26</modified>
    Task<PagedResult<PartnerSummaryResponse>> SearchAsync(PartnerSearchQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Get one complete partner of the given type by id or identifier, active or inactive
    /// </summary>
    /// <param name="query">Type and id or identifier, already validated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The partner, or <see langword="null"/> when no partner of that type matches</returns>
    /// <author>Ricardo Gomes</author>
    /// <created>2026-10-26</created>
    /// <modified>2026-10-26</modified>
    Task<PartnerResponse?> GetDetailsAsync(PartnerDetailsQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Replace all data of a partner, including status and the e-mail, phone and contact lists. Contacts sent
    /// with an identifier are kept and updated, contacts without it are created and the missing ones are deleted.
    /// </summary>
    /// <param name="identifier">Partner public identifier</param>
    /// <param name="request">New partner data, already validated for presence and length</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated partner, <see cref="PartnerResultStatus.NotFound"/> or <see cref="PartnerResultStatus.DuplicateDocument"/></returns>
    /// <exception cref="DomainException">A value breaks a partner rule, or a contact identifier does not belong to the partner</exception>
    /// <author>Ricardo Gomes</author>
    /// <created>2026-10-26</created>
    /// <modified>2026-10-26</modified>
    Task<PartnerResult> UpdateAsync(Guid identifier, UpdatePartnerRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deactivate a partner (logical delete), keeping all its data. Deactivating an inactive partner does nothing.
    /// </summary>
    /// <param name="identifier">Partner public identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the partner exists</returns>
    /// <author>Ricardo Gomes</author>
    /// <created>2026-10-26</created>
    /// <modified>2026-10-26</modified>
    Task<bool> DeactivateAsync(Guid identifier, CancellationToken cancellationToken);
}
