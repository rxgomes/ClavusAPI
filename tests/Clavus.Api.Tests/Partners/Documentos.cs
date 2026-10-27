namespace Clavus.Api.Tests.Partners;

/// <summary>
/// Gera CPFs válidos e diferentes entre si, para que os testes não colidam no índice único do documento.
/// </summary>
internal static class Documentos
{
    public static string NovoCpf()
    {
        while (true)
        {
            var digitos = Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(10)).ToList();
            if (digitos.Distinct().Count() == 1)
            {
                continue;
            }

            digitos.Add(DigitoVerificador(digitos));
            digitos.Add(DigitoVerificador(digitos));

            return string.Concat(digitos);
        }
    }

    private static int DigitoVerificador(List<int> digitos)
    {
        var soma = digitos.Select((digito, indice) => digito * (digitos.Count + 1 - indice)).Sum();
        var resto = soma % 11;

        return resto < 2 ? 0 : 11 - resto;
    }
}
