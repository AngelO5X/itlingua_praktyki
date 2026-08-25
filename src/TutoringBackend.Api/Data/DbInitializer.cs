using Microsoft.AspNetCore.Identity;
using TutoringBackend.Api.Common;
using TutoringBackend.Api.Models;

namespace TutoringBackend.Api.Data;

/// Tworzy role Identity oraz pierwsze konto administratora przy starcie aplikacji.
/// Hasło admina NIE jest hardcodowane w kodzie - pobierane wyłącznie ze zmiennej
/// środowiskowej / configuration (żeby nie wyciekło do repozytorium).
public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var adminEmail = config["SeedAdmin:Email"];
        var adminPassword = config["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            // Brak skonfigurowanego admina - świadomie nie tworzymy konta z domyślnym hasłem.
            return;
        }

        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing is not null) return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "Admin",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }
}
