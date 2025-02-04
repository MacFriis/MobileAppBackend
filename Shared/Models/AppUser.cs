using Microsoft.AspNetCore.Identity;

namespace Shared.Models;

public class AppUser: IdentityUser
{
    public string? RefreshToken;
    public DateTimeOffset RefreshTokenExpiryTime;
    public PersonNameComponents NameComponents { get; set; } = new();
    public PostalAddress? Address { get; set; }
    public NotificationSetting? NotificationSetting { get; set; }
}