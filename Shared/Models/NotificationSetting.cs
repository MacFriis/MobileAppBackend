namespace Shared.Models;

public class NotificationSetting
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; }
    public List<DeviceToken> DeviceTokens { get; set; } = new();
}

public class DeviceToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
}