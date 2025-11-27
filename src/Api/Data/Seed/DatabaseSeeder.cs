using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Services.Administration;
using Services.Financial;
using Services.Inventory;
using Services.Purchasing;
using Services.Sales;
using Services.Security;

namespace Api.Data.Seed
{
    public class DatabaseSeeder
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Seed()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var adminService = scope.ServiceProvider.GetRequiredService<IAdministrationService>();
                var financialService = scope.ServiceProvider.GetRequiredService<IFinancialService>();
                var salesService = scope.ServiceProvider.GetRequiredService<ISalesService>();
                var purchasingService = scope.ServiceProvider.GetRequiredService<IPurchasingService>();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var initializer = new Initializer(
                    adminService,
                    financialService,
                    salesService,
                    purchasingService,
                    inventoryService,
                    securityService
                );

                // First, setup security roles (step 11 moved to BEFORE admin user creation)
                Console.WriteLine("=== Setting up Security Roles ===");
                securityService.AddRole("SystemAdministrators");
                securityService.AddRole("GeneralUsers");
                Console.WriteLine("✅ Security roles created");

                // NOW create admin user (after roles exist)
                Console.WriteLine("=== Starting Admin User Seeding ===");
                SeedDefaultAdminUser(userManager, adminService).Wait();
                Console.WriteLine("=== Admin User Seeding Completed ===");

                // Run the rest of setup (but skip security roles since we already did them)
                var success = initializer.Setup();

                if (success)
                {
                    Console.WriteLine("Database seeding completed successfully.");
                }
                else
                {
                    Console.WriteLine("Database seeding failed. Check logs for details.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Seed(): {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task SeedDefaultAdminUser(UserManager<ApplicationUser> userManager, IAdministrationService adminService)
        {
            const string adminEmail = "admin@accountgo.ph";
            const string adminPassword = "P@ssword1";

            try
            {
                Console.WriteLine($"Checking if user '{adminEmail}' exists...");
                var existingUser = await userManager.FindByEmailAsync(adminEmail);
                if (existingUser != null)
                {
                    Console.WriteLine($"✅ Admin user '{adminEmail}' already exists in Identity, skipping creation.");
                    return;
                }

                Console.WriteLine($"Creating default admin user '{adminEmail}' in Identity...");
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    Console.WriteLine($"✅ Default admin user '{adminEmail}' created successfully in Identity.");

                    // Also create in application domain if not exists
                    try
                    {
                        var domainUser = new Core.Domain.Security.User
                        {
                            EmailAddress = adminEmail,
                            UserName = adminEmail,
                            Firstname = "Admin",
                            Lastname = "User"
                        };

                        adminService.SaveUser(domainUser);
                        Console.WriteLine($"✅ Default admin user created in application domain.");
                    }
                    catch (Exception domainEx)
                    {
                        Console.WriteLine($"⚠️  Domain user creation failed (may already exist): {domainEx.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Failed to create default admin user:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"   - {error.Code}: {error.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding default admin user: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}