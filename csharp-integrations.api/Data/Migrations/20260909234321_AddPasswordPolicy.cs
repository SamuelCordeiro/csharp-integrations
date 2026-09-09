using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace csharp_integrations.api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MinimumLength = table.Column<int>(type: "INTEGER", nullable: false),
                    RequireUppercase = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireLowercase = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireDigit = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireNonAlphanumeric = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiredUniqueCharacters = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxFailedAccessAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LockoutDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordPolicies", x => x.Id);
                    table.CheckConstraint("CK_PasswordPolicies_LockoutDurationMinutes", "LockoutDurationMinutes >= 1");
                    table.CheckConstraint("CK_PasswordPolicies_MaxFailedAccessAttempts", "MaxFailedAccessAttempts >= 1");
                    table.CheckConstraint("CK_PasswordPolicies_MinimumLength", "MinimumLength >= 8");
                    table.CheckConstraint("CK_PasswordPolicies_RequiredUniqueCharacters", "RequiredUniqueCharacters >= 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordPolicies");
        }
    }
}
