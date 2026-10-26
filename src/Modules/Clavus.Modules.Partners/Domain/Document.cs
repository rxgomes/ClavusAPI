using Clavus.Shared.Abstractions.Domain;

namespace Clavus.Modules.Partners.Domain;

/// <summary>
/// Partner document: CPF (11 digits) or CNPJ (14 characters, numeric or alphanumeric), validated by its
/// check digits and kept without formatting
/// </summary>
public sealed record Document
{
    private const int CpfLength = 11;

    private const int CnpjLength = 14;

    private static readonly char[] Formatting = ['.', '-', '/', ' '];

    private static readonly int[] CpfFirstWeights = [10, 9, 8, 7, 6, 5, 4, 3, 2];

    private static readonly int[] CpfSecondWeights = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

    private static readonly int[] CnpjFirstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    private static readonly int[] CnpjSecondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>
    /// Create a document from a CPF or CNPJ, formatted or not
    /// </summary>
    /// <param name="value">CPF or CNPJ; dots, dashes, slashes and spaces are removed and letters are uppercased</param>
    /// <exception cref="DomainException">Empty or invalid document</exception>
    public Document(string? value)
    {
        var normalized = Text.RemoveFormatting(value, Formatting).ToUpperInvariant();
        if (normalized.Length == 0)
        {
            throw new DomainException("O campo documento é obrigatório.");
        }

        if (normalized.Length == CpfLength)
        {
            if (!IsValidCpf(normalized))
            {
                throw new DomainException("CPF inválido.");
            }
        }
        else if (normalized.Length == CnpjLength)
        {
            if (!IsValidCnpj(normalized))
            {
                throw new DomainException("CNPJ inválido.");
            }
        }
        else
        {
            throw new DomainException("Documento inválido: informe um CPF com 11 dígitos ou um CNPJ com 14 caracteres.");
        }

        Value = normalized;
    }

    /// <summary>
    /// Document without formatting (11 digits for CPF, 14 characters for CNPJ)
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Document kind, derived from its length
    /// </summary>
    public DocumentType Type => Value.Length == CpfLength ? DocumentType.Individual : DocumentType.Company;

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }

    private static bool IsValidCpf(string cpf)
    {
        if (!Text.IsDigits(cpf) || AllEqual(cpf))
        {
            return false;
        }

        var firstDigit = CheckDigit(cpf, CpfFirstWeights);
        var secondDigit = CheckDigit(cpf, CpfSecondWeights);

        return cpf[9] - '0' == firstDigit && cpf[10] - '0' == secondDigit;
    }

    private static bool IsValidCnpj(string cnpj)
    {
        var baseIsAlphanumeric = cnpj[..12].All(character => char.IsAsciiDigit(character) || char.IsAsciiLetterUpper(character));
        if (!baseIsAlphanumeric || !Text.IsDigits(cnpj[12..]) || AllEqual(cnpj))
        {
            return false;
        }

        var firstDigit = CheckDigit(cnpj, CnpjFirstWeights);
        var secondDigit = CheckDigit(cnpj, CnpjSecondWeights);

        return cnpj[12] - '0' == firstDigit && cnpj[13] - '0' == secondDigit;
    }

    /// <summary>
    /// Modulo 11 check digit over the first <c>weights.Length</c> characters. Each character is worth its ASCII
    /// code minus 48, which keeps digits as 0-9 and makes the rule valid for the alphanumeric CNPJ.
    /// </summary>
    private static int CheckDigit(string value, int[] weights)
    {
        var sum = 0;
        for (var index = 0; index < weights.Length; index++)
        {
            sum += (value[index] - '0') * weights[index];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static bool AllEqual(string value)
    {
        return value.All(character => character == value[0]);
    }
}
