using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace Shared.Data;

public class AppDbContext: IdentityDbContext<AppUser>
{
    public DbSet<NotificationSetting> NotificationSettings { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options) { }
}