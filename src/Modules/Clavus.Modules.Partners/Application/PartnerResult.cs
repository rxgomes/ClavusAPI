using Clavus.Modules.Partners.Contracts;

namespace Clavus.Modules.Partners.Application;

/// <summary>
/// Outcome of a partner create or update
/// </summary>
public enum PartnerResultStatus
{
    /// <summary>
    /// The partner was saved
    /// </summary>
    Success,

    /// <summary>
    /// No partner has the given identifier
    /// </summary>
    NotFound,

    /// <summary>
    /// Another partner, active or inactive, already has the document
    /// </summary>
    DuplicateDocument,
}

/// <summary>
/// Result of a partner create or update: the saved partner, or the reason it was not saved
/// </summary>
/// <param name="Status">Outcome</param>
/// <param name="Partner">Saved partner, present only when <paramref name="Status"/> is <see cref="PartnerResultStatus.Success"/></param>
public sealed record PartnerResult(PartnerResultStatus Status, PartnerResponse? Partner = null)
{
    /// <summary>
    /// No partner has the given identifier
    /// </summary>
    public static readonly PartnerResult NotFound = new(PartnerResultStatus.NotFound);

    /// <summary>
    /// Another partner already has the document
    /// </summary>
    public static readonly PartnerResult DuplicateDocument = new(PartnerResultStatus.DuplicateDocument);

    /// <summary>
    /// The partner was saved
    /// </summary>
    /// <param name="partner">Saved partner</param>
    /// <returns>Successful result</returns>
    public static PartnerResult Success(PartnerResponse partner)
    {
        return new PartnerResult(PartnerResultStatus.Success, partner);
    }
}
