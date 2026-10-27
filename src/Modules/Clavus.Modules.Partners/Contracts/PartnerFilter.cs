using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Clavus.Modules.Partners.Contracts;

/// <summary>
/// Filters shared by every partner search: exactly one partner type (client or supplier) and, optionally,
/// the partner id or identifier (never both)
/// </summary>
public abstract record PartnerFilter : IValidatableObject
{
    /// <summary>
    /// Search clients. Send <c>isClient=true</c> or <c>isSupplier=true</c>, never both.
    /// </summary>
    [FromQuery(Name = "isClient")]
    public bool? IsClient { get; init; }

    /// <summary>
    /// Search suppliers. Send <c>isClient=true</c> or <c>isSupplier=true</c>, never both.
    /// </summary>
    [FromQuery(Name = "isSupplier")]
    public bool? IsSupplier { get; init; }

    /// <summary>
    /// Partner id. Send <c>id</c> or <c>identifier</c>, never both.
    /// </summary>
    [FromQuery(Name = "id")]
    public long? Id { get; init; }

    /// <summary>
    /// Partner public identifier. Send <c>id</c> or <c>identifier</c>, never both.
    /// </summary>
    [FromQuery(Name = "identifier")]
    public Guid? Identifier { get; init; }

    /// <summary>
    /// True when the search is for clients; false when it is for suppliers. Valid only after validation.
    /// </summary>
    internal bool SearchesClients => IsClient == true;

    /// <inheritdoc />
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var onlyOneType = (IsClient == true) != (IsSupplier == true) && IsClient != false && IsSupplier != false;
        if (!onlyOneType)
        {
            yield return new ValidationResult(
                "Informe isClient=true ou isSupplier=true, nunca os dois.",
                [nameof(IsClient), nameof(IsSupplier)]);
        }

        if (Id is not null && Identifier is not null)
        {
            yield return new ValidationResult(
                "Informe id ou identifier, nunca os dois.",
                [nameof(Id), nameof(Identifier)]);
        }
    }
}

/// <summary>
/// Paged partner search
/// </summary>
public sealed record PartnerSearchQuery : PartnerFilter
{
    /// <summary>
    /// Default <see cref="Page"/>
    /// </summary>
    public const int DefaultPage = 1;

    /// <summary>
    /// Default <see cref="PageSize"/>
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Maximum <see cref="PageSize"/>
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Search active (<see langword="true"/>, default) or inactive (<see langword="false"/>) partners
    /// </summary>
    [FromQuery(Name = "isActive")]
    public bool? IsActive { get; init; }

    /// <summary>
    /// Page number, starting at 1 (default 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "A página deve ser maior ou igual a {1}.")]
    [FromQuery(Name = "page")]
    public int? Page { get; init; }

    /// <summary>
    /// Items per page, between 1 and 100 (default 20)
    /// </summary>
    [Range(1, MaxPageSize, ErrorMessage = "O tamanho da página deve estar entre {1} e {2}.")]
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; init; }
}

/// <summary>
/// Search of one partner, by id or identifier (one of them is required)
/// </summary>
public sealed record PartnerDetailsQuery : PartnerFilter
{
    /// <inheritdoc />
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in base.Validate(validationContext))
        {
            yield return result;
        }

        if (Id is null && Identifier is null)
        {
            yield return new ValidationResult(
                "Informe id ou identifier.",
                [nameof(Id), nameof(Identifier)]);
        }
    }
}
