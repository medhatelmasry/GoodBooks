using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations.ApiDb
{
    public partial class SeedSecurityAndAuditData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Audit 설정
            migrationBuilder.Sql(@"
                INSERT INTO [dbo].[AuditableEntity] ([EntityName], [EnableAudit]) VALUES ('Company', 1);
                DECLARE @auditableEntityId INT;
                SELECT @auditableEntityId = [Id] FROM [dbo].[AuditableEntity] WHERE [EntityName] = 'Company';

                INSERT INTO [dbo].[AuditableAttribute] ([AuditableEntityId], [AttributeName], [EnableAudit])
                VALUES
                    (@auditableEntityId, 'CompanyCode', 1),
                    (@auditableEntityId, 'Name', 1),
                    (@auditableEntityId, 'ShortName', 1),
                    (@auditableEntityId, 'CRA', 1);
            ");

            // SecurityGroup
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [SecurityGroup])
                BEGIN
                    SET IDENTITY_INSERT [SecurityGroup] ON;

                    INSERT INTO [SecurityGroup] (Id, Name, DisplayName)
                    VALUES
                        (1, 'AccountsReceivable', 'Accounts Receivable'),
                        (2, 'AccountsPayable', 'Accounts Payable'),
                        (3, 'Financials', 'Financials'),
                        (4, 'SystemAdministration', 'System Administration');

                    SET IDENTITY_INSERT [SecurityGroup] OFF;
                END
            ");

            // SecurityRole
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [SecurityRole])
                BEGIN
                    SET IDENTITY_INSERT [SecurityRole] ON;

                    INSERT INTO [SecurityRole] (Id, Name, DisplayName, SysAdmin, [System])
                    VALUES
                        (1, 'SystemAdministrators', 'System Administrators', 1, 1),
                        (2, 'GeneralUsers', 'General Users', 0, 1);

                    SET IDENTITY_INSERT [SecurityRole] OFF;
                END
            ");

            // SecurityPermission
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [SecurityPermission])
                BEGIN
                    SET IDENTITY_INSERT [SecurityPermission] ON;

                    INSERT INTO [SecurityPermission] (Id, Name, DisplayName, SecurityGroupId)
                    VALUES
                        (1, 'ManageUsers', 'Manage Users', 1);

                    SET IDENTITY_INSERT [SecurityPermission] OFF;
                END
            ");

            // 초기 사용자 추가
            migrationBuilder.Sql(@"
                INSERT [dbo].[AspNetUsers]
                    ([Id], [AccessFailedCount], [ConcurrencyStamp], [Email], [EmailConfirmed],
                    [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName],
                    [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [SecurityStamp],
                    [TwoFactorEnabled], [UserName])
                VALUES
                    (N'c2a1983a-6e3f-40a2-b141-0a4e827af44e', 0, N'a1f8ccbc-f77a-4cd3-8d76-f24ed7be2d03',
                     N'admin@accountgo.ph', 0, 1, NULL, N'ADMIN@ACCOUNTGO.PH', N'ADMIN@ACCOUNTGO.PH',
                     N'AQAAAAEAACcQAAAAEOxDmtWUR4F6ZycBAXzB0Wz5c0yduXEQVIgZwGPEOKRdfKq1dmqleAPMjvInBp+xow==',
                     NULL, 0, N'544b121a-1973-4403-9a6f-5a6abcec3bf5', 0, N'admin@accountgo.ph');

                SET IDENTITY_INSERT [dbo].[User] ON;

                INSERT [dbo].[User] ([Id], [Lastname], [Firstname], [UserName], [EmailAddress])
                VALUES (1, 'System', 'Administrator', N'admin@accountgo.ph', N'admin@accountgo.ph');

                SET IDENTITY_INSERT [dbo].[User] OFF;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [dbo].[User] WHERE Id = 1");
            migrationBuilder.Sql("DELETE FROM [dbo].[AspNetUsers] WHERE [UserName] = 'admin@accountgo.ph'");
            migrationBuilder.Sql("DELETE FROM [SecurityPermission]");
            migrationBuilder.Sql("DELETE FROM [SecurityRole]");
            migrationBuilder.Sql("DELETE FROM [SecurityGroup]");
            migrationBuilder.Sql("DELETE FROM [AuditableAttribute] WHERE [AuditableEntityId] IN (SELECT Id FROM [AuditableEntity] WHERE [EntityName] = 'Company')");
            migrationBuilder.Sql("DELETE FROM [AuditableEntity] WHERE [EntityName] = 'Company'");
        }
    }
}
