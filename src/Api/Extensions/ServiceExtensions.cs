﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Api.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureJWT(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = Environment.GetEnvironmentVariable("SECRET");
            
            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings["validIssuer"],
                    ValidAudience = jwtSettings["validAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });
        }

        public static void ConfigureSqlContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // These environment variables can be overriden from launchSettings.json.
            string dbServer = System.Environment.GetEnvironmentVariable("DBSERVER") ?? "localhost,1444";
            string dbUserID = System.Environment.GetEnvironmentVariable("DBUSERID") ?? "sa";
            string dbUserPassword = System.Environment.GetEnvironmentVariable("DBPASSWORD") ?? "SqlPassword!";
            string dbName = System.Environment.GetEnvironmentVariable("DBNAME") ?? "accountgodb";

            connectionString = String.Format(configuration.GetConnectionString("DefaultConnection")!, dbServer, dbUserID, dbUserPassword, dbName);

            System.Console.WriteLine("DB Connection String: " + connectionString);
            
            services
                //.AddEntityFrameworkSqlServer()
                .AddDbContext<ApiDbContext>(options => options.UseSqlServer(connectionString))
                //.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery) // Add this line
                .AddDbContext<ApplicationIdentityDbContext>(options => options.UseSqlServer(connectionString));
        }
    }
}