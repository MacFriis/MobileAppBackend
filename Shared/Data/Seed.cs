using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;

namespace Shared.Data;

public class Seed
{
    public static async Task SeedAdmin(IServiceProvider serviceProvider, string roleName, string userName, string email, string password)
    {
        var cbContext = serviceProvider.GetRequiredService<AppDbContext>();
        if (cbContext.Users.Any()) return;
        
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));

            var user = new AppUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, roleName);
    }
}