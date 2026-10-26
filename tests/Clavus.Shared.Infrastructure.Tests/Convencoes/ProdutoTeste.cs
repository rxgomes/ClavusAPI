using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Shared.Infrastructure.Tests.Convencoes;

public sealed class ProdutoTeste : Entity
{
    public string Nome { get; set; } = string.Empty;
}
