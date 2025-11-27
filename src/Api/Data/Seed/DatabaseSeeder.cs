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
                
                ApplicationUser adminUser;
                
                if (existingUser != null)
                {
                    Console.WriteLine($"✅ Admin user '{adminEmail}' already exists in Identity.");
                    adminUser = existingUser;
                }
                else
                {
                    Console.WriteLine($"Creating default admin user '{adminEmail}' in Identity...");
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);

                    if (result.Succeeded)
                    {
                        Console.WriteLine($"✅ Default admin user '{adminEmail}' created successfully in Identity.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create default admin user:");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"   - {error.Code}: {error.Description}");
                        }
                        return;
                    }
                }

                // Ensure user is in SystemAdministrators role in Identity
                var userRolesInIdentity = await userManager.GetRolesAsync(adminUser);
                if (!userRolesInIdentity.Contains("SystemAdministrators"))
                {
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "SystemAdministrators");
                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine($"✅ User '{adminEmail}' added to SystemAdministrators role in Identity.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to add admin user to SystemAdministrators role in Identity:");
                        foreach (var error in roleResult.Errors)
                        {
                            Console.WriteLine($"   - {error.Code}: {error.Description}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"✅ User '{adminEmail}' already in SystemAdministrators role in Identity.");
                }

                // Ensure user exists and has role in application domain
                try
                {
                    var savedUser = securityService.GetUser(adminEmail);
                    
                    if (savedUser == null)
                    {
                        Console.WriteLine($"Creating user '{adminEmail}' in application domain...");
                        var domainUser = new Core.Domain.Security.User
                        {
                            EmailAddress = adminEmail,
                            UserName = adminEmail,
                            Firstname = "Admin",
                            Lastname = "User"
                        };

                        adminService.SaveUser(domainUser);
                        Console.WriteLine($"✅ User '{adminEmail}' created in application domain.");
                        
                        savedUser = securityService.GetUser(adminEmail);
                    }
                    else
                    {
                        Console.WriteLine($"✅ User '{adminEmail}' already exists in application domain.");
                    }
                    
                    // Ensure user is in SystemAdministrators role in application domain
                    var adminRole = securityService.GetAllSecurityRole().FirstOrDefault(r => r.Name == "SystemAdministrators");
                    
                    if (savedUser != null && adminRole != null)
                    {
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
                
                ApplicationUser generalUser;
                
                if (existingUser != null)
                {
                    Console.WriteLine($"✅ General user '{userEmail}' already exists in Identity.");
                    generalUser = existingUser;
                }
                else
                {
                    Console.WriteLine($"Creating default general user '{userEmail}' in Identity...");
                    generalUser = new ApplicationUser
                    {
                        UserName = userEmail,
                        Email = userEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(generalUser, userPassword);

                    if (result.Succeeded)
                    {
                        Console.WriteLine($"✅ Default general user '{userEmail}' created successfully in Identity.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create default general user:");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"   - {error.Code}: {error.Description}");
                        }
                        return;
                    }
                }

                // Ensure user is in GeneralUsers role in Identity
                var userRolesInIdentity = await userManager.GetRolesAsync(generalUser);
                if (!userRolesInIdentity.Contains("GeneralUsers"))
                {
                    var roleResult = await userManager.AddToRoleAsync(generalUser, "GeneralUsers");
                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine($"✅ User '{userEmail}' added to GeneralUsers role in Identity.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to add user to GeneralUsers role in Identity:");
                        foreach (var error in roleResult.Errors)
                        {
                            Console.WriteLine($"   - {error.Code}: {error.Description}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"✅ User '{userEmail}' already in GeneralUsers role in Identity.");
                }

                // Ensure user exists and has role in application domain
                try
                {
                    var savedUser = securityService.GetUser(userEmail);
                    
                    if (savedUser == null)
                    {
                        Console.WriteLine($"Creating user '{userEmail}' in application domain...");
                        var domainUser = new Core.Domain.Security.User
                        {
                            EmailAddress = userEmail,
                            UserName = userEmail,
                            Firstname = "Regular",
                            Lastname = "User"
                        };

                        adminService.SaveUser(domainUser);
                        Console.WriteLine($"✅ User '{userEmail}' created in application domain.");
                        
                        savedUser = securityService.GetUser(userEmail);
                    }
                    else
                    {
                        Console.WriteLine($"✅ User '{userEmail}' already exists in application domain.");
                    }
                    
                    // Ensure user is in GeneralUsers role in application domain
                    var generalRole = securityService.GetAllSecurityRole().FirstOrDefault(r => r.Name == "GeneralUsers");
                    
                    if (savedUser != null && generalRole != null)
                    {
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding default general user: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}