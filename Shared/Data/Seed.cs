using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;

namespace Shared.Data;

public class AppDbContextSeed
{
    public static async Task Seed(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        var adminUser = await userManager.FindByEmailAsync("dashboard@friisconsult.com");
        if (adminUser == null)
        {
            var user = new AppUser
            {
                UserName = "Admin",
                Email = "dashboard@friisconsult.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Admin123!");
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}