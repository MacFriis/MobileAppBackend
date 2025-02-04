namespace Shared.Models;

public class DashboardUser
{
    public PersonNameComponents NameCom { get; set; } = new();
    public PostalAddress? Address { get; set; }
    public NotificationSetting? NotificationSetting { get; set; }
}