namespace Clavus.Modules.Partners.Domain;

/// <summary>
/// Kind of partner document, which defines whether the partner is an individual or a company
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// CPF (11 digits): individual (pessoa física)
    /// </summary>
    Individual,

    /// <summary>
    /// CNPJ (14 characters, numeric or alphanumeric): company (pessoa jurídica)
    /// </summary>
    Company,
}
