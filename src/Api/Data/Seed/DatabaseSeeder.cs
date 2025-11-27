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
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                var initializer = new Initializer(
                    adminService,
                    financialService,
                    salesService,
                    purchasingService,
                    inventoryService,
                    securityService
                );

                // First, setup security roles in Identity (AspNetRoles table)
                Console.WriteLine("=== Setting up Identity Roles ===");
                SeedIdentityRoles(roleManager).Wait();
                Console.WriteLine("✅ Identity roles created");

                // Also setup security roles in application domain
                Console.WriteLine("=== Setting up Application Security Roles ===");
                securityService.AddRole("SystemAdministrators");
                securityService.AddRole("GeneralUsers");
                Console.WriteLine("✅ Application security roles created");

                // NOW create admin user (after roles exist)
                Console.WriteLine("=== Starting Admin User Seeding ===");
                SeedDefaultAdminUser(userManager, adminService, securityService).Wait();
                Console.WriteLine("=== Admin User Seeding Completed ===");

                // Create a test general user
                Console.WriteLine("=== Starting General User Seeding ===");
                SeedDefaultGeneralUser(userManager, adminService, securityService).Wait();
                Console.WriteLine("=== General User Seeding Completed ===");

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

        private async Task SeedIdentityRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "SystemAdministrators", "GeneralUsers" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    Console.WriteLine($"Creating Identity role '{roleName}'...");
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"✅ Identity role '{roleName}' created successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create Identity role '{roleName}':");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"   - {error.Code}: {error.Description}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"✅ Identity role '{roleName}' already exists.");
                }
            }
        }

        private async Task SeedDefaultAdminUser(UserManager<ApplicationUser> userManager, IAdministrationService adminService, ISecurityService securityService)
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

                    // Add to SystemAdministrators role
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "SystemAdministrators");
                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine($"✅ User '{adminEmail}' added to SystemAdministrators role.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to add admin user to SystemAdministrators role:");
                        foreach (var error in roleResult.Errors)
                        {
                            Console.WriteLine($"   - {error.Code}: {error.Description}");
                        }
                    }

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
                        
                        // Assign to SystemAdministrators role in application domain
                        var savedUser = securityService.GetUser(adminEmail);
                        var adminRole = securityService.GetAllSecurityRole().FirstOrDefault(r => r.Name == "SystemAdministrators");
                        
                        if (savedUser != null && adminRole != null)
                        {
                            // Check if user is already in role
                            var userRoles = securityService.GetRolesForUser(adminEmail);
                            if (!userRoles.Any(ur => ur.SecurityRoleId == adminRole.Id))
                            {
                                securityService.AddUserInRole(savedUser.Id, adminRole.Id);
                                Console.WriteLine($"✅ User '{adminEmail}' added to SystemAdministrators role in application domain.");
                            }
                            else
                            {
                                Console.WriteLine($"✅ User '{adminEmail}' already in SystemAdministrators role in application domain.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️  Could not assign role: User or role not found.");
                        }
                    }
                    catch (Exception domainEx)
                    {
                        Console.WriteLine($"⚠️  Domain user creation/role assignment failed: {domainEx.Message}");
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

        private async Task SeedDefaultGeneralUser(UserManager<ApplicationUser> userManager, IAdministrationService adminService, ISecurityService securityService)
        {
            const string userEmail = "user@accountgo.ph";
            const string userPassword = "P@ssword1";

            try
            {
                Console.WriteLine($"Checking if user '{userEmail}' exists...");
                var existingUser = await userManager.FindByEmailAsync(userEmail);
                if (existingUser != null)
                {
                    Console.WriteLine($"✅ General user '{userEmail}' already exists in Identity, skipping creation.");
                    return;
                }

                Console.WriteLine($"Creating default general user '{userEmail}' in Identity...");
                var generalUser = new ApplicationUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(generalUser, userPassword);

                if (result.Succeeded)
                {
                    Console.WriteLine($"✅ Default general user '{userEmail}' created successfully in Identity.");

                    // Add to GeneralUsers role
                    var roleResult = await userManager.AddToRoleAsync(generalUser, "GeneralUsers");
                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine($"✅ User '{userEmail}' added to GeneralUsers role.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to add user to GeneralUsers role:");
                        foreach (var error in roleResult.Errors)
                        {
                            Console.WriteLine($"   - {error.Code}: {error.Description}");
                        }
                    }

                    // Also create in application domain if not exists
                    try
                    {
                        var domainUser = new Core.Domain.Security.User
                        {
                            EmailAddress = userEmail,
                            UserName = userEmail,
                            Firstname = "Regular",
                            Lastname = "User"
                        };

                        adminService.SaveUser(domainUser);
                        Console.WriteLine($"✅ Default general user created in application domain.");
                        
                        // Assign to GeneralUsers role in application domain
                        var savedUser = securityService.GetUser(userEmail);
                        var generalRole = securityService.GetAllSecurityRole().FirstOrDefault(r => r.Name == "GeneralUsers");
                        
                        if (savedUser != null && generalRole != null)
                        {
                            // Check if user is already in role
                            var userRoles = securityService.GetRolesForUser(userEmail);
                            if (!userRoles.Any(ur => ur.SecurityRoleId == generalRole.Id))
                            {
                                securityService.AddUserInRole(savedUser.Id, generalRole.Id);
                                Console.WriteLine($"✅ User '{userEmail}' added to GeneralUsers role in application domain.");
                            }
                            else
                            {
                                Console.WriteLine($"✅ User '{userEmail}' already in GeneralUsers role in application domain.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️  Could not assign role: User or role not found.");
                        }
                    }
                    catch (Exception domainEx)
                    {
                        Console.WriteLine($"⚠️  Domain user creation/role assignment failed: {domainEx.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Failed to create default general user:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"   - {error.Code}: {error.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding default general user: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}