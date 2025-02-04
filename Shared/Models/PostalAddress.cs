namespace Shared.Models;

public class PostalAddress
{
    public Guid Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string IsoCountryCode { get; set; } = string.Empty;
    public string SubAdministrativeArea { get; set; } = string.Empty;
    public string SubLocality { get; set; } = string.Empty;
}