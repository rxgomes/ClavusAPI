using System.ComponentModel.DataAnnotations;
using Clavus.Modules.Partners.Application;
using Clavus.Modules.Partners.Contracts;
using Clavus.Shared.Abstractions.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Clavus.Modules.Partners.Endpoints;

/// <summary>
/// HTTP endpoints of the partner registration (<c>/partners</c>)
/// </summary>
public static class PartnerEndpoints
{
    private const string DuplicateDocumentMessage = "Já existe um parceiro com o documento informado.";

    /// <summary>
    /// Map the <c>/partners</c> endpoints
    /// </summary>
    /// <param name="endpoints">Host endpoint route builder</param>
    /// <returns>The same endpoint route builder</returns>
    public static IEndpointRouteBuilder MapPartnerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/partners").WithTags("Parceiros");

        group.MapPost("/", CreateAsync)
            .WithName("CreatePartner")
            .WithSummary("Incluir parceiro")
            .WithDescription("Inclui um parceiro com endereço, e-mails, telefones e contatos. O parceiro deve ser cliente, fornecedor ou ambos.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", SearchAsync)
            .WithName("SearchPartners")
            .WithSummary("Pesquisar parceiros")
            .WithDescription("Pesquisa paginada, ordenada por nome fantasia. Informe isClient=true ou isSupplier=true (nunca os dois) e, opcionalmente, id ou identifier (nunca os dois).")
            .ProducesValidationProblem();

        group.MapGet("/details", GetDetailsAsync)
            .WithName("GetPartnerDetails")
            .WithSummary("Consultar parceiro")
            .WithDescription("Devolve o parceiro completo, ativo ou inativo. Informe isClient=true ou isSupplier=true (nunca os dois) e id ou identifier (nunca os dois).")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{identifier:guid}", UpdateAsync)
            .WithName("UpdatePartner")
            .WithSummary("Alterar parceiro")
            .WithDescription("Substitui todos os dados do parceiro, incluindo a situação e as listas de e-mails, telefones e contatos. Contatos enviados com identifier são mantidos.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{identifier:guid}", DeactivateAsync)
            .WithName("DeactivatePartner")
            .WithSummary("Inativar parceiro")
            .WithDescription("Exclusão lógica: marca o parceiro como inativo, sem excluir dados.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<Results<Created<PartnerResponse>, ProblemHttpResult>> CreateAsync(
        CreatePartnerRequest request,
        IPartnerService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        if (result.Status == PartnerResultStatus.DuplicateDocument)
        {
            return DuplicateDocument();
        }

        return TypedResults.Created((string?)null, result.Partner);
    }

    private static async Task<Results<Ok<PagedResult<PartnerSummaryResponse>>, ValidationProblem>> SearchAsync(
        [AsParameters] PartnerSearchQuery query,
        IPartnerService service,
        CancellationToken cancellationToken)
    {
        if (Invalid(query) is { } validationProblem)
        {
            return validationProblem;
        }

        return TypedResults.Ok(await service.SearchAsync(query, cancellationToken));
    }

    private static async Task<Results<Ok<PartnerResponse>, ValidationProblem, ProblemHttpResult>> GetDetailsAsync(
        [AsParameters] PartnerDetailsQuery query,
        IPartnerService service,
        CancellationToken cancellationToken)
    {
        if (Invalid(query) is { } validationProblem)
        {
            return validationProblem;
        }

        var partner = await service.GetDetailsAsync(query, cancellationToken);
        if (partner is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(partner);
    }

    private static async Task<Results<Ok<PartnerResponse>, ProblemHttpResult>> UpdateAsync(
        Guid identifier,
        UpdatePartnerRequest request,
        IPartnerService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(identifier, request, cancellationToken);

        return result.Status switch
        {
            PartnerResultStatus.NotFound => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound),
            PartnerResultStatus.DuplicateDocument => DuplicateDocument(),
            _ => TypedResults.Ok(result.Partner),
        };
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeactivateAsync(
        Guid identifier,
        IPartnerService service,
        CancellationToken cancellationToken)
    {
        if (!await service.DeactivateAsync(identifier, cancellationToken))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.NoContent();
    }

    private static ProblemHttpResult DuplicateDocument()
    {
        return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: DuplicateDocumentMessage);
    }

    /// <summary>
    /// Validate the query parameters (attributes and cross-field rules), so the rules hold even where the
    /// built-in validation does not cover <c>[AsParameters]</c>
    /// </summary>
    /// <returns>The validation problem, or <see langword="null"/> when the query is valid</returns>
    private static ValidationProblem? Invalid(PartnerFilter query)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(query, new ValidationContext(query), results, validateAllProperties: true))
        {
            return null;
        }

        var errors = results
            .SelectMany(result => result.MemberNames.Select(member => (Member: member, Message: result.ErrorMessage ?? string.Empty)))
            .GroupBy(error => error.Member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).Distinct().ToArray());

        return TypedResults.ValidationProblem(errors);
    }
}
