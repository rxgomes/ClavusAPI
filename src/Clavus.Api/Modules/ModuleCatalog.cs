using Clavus.Shared.Abstractions;

namespace Clavus.Api.Modules;

/// <summary>
/// Lista explícita dos módulos de negócio compostos pelo host.
/// Para adicionar um módulo, inclua uma instância dele em <see cref="All"/>.
/// </summary>
internal static class ModuleCatalog
{
    public static readonly IReadOnlyList<IModule> All =
    [
    ];
}
