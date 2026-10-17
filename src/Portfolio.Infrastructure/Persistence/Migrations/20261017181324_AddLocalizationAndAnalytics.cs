using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizationAndAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bio_en",
                table: "site_profiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "headline_en",
                table: "site_profiles",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "body_en",
                table: "projects",
                type: "character varying(50000)",
                maxLength: 50000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "summary_en",
                table: "projects",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_en",
                table: "projects",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_en",
                table: "experiences",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_en",
                table: "experiences",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name_en",
                table: "categories",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "language",
                table: "blog_posts",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "tr");

            migrationBuilder.CreateTable(
                name: "page_views",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    referrer_host = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    visitor_hash = table.Column<string>(type: "character(16)", fixedLength: true, maxLength: 16, nullable: false),
                    device = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_views", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_page_views_day",
                table: "page_views",
                column: "day");

            migrationBuilder.CreateIndex(
                name: "ix_page_views_day_path",
                table: "page_views",
                columns: new[] { "day", "path" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "page_views");

            migrationBuilder.DropColumn(
                name: "bio_en",
                table: "site_profiles");

            migrationBuilder.DropColumn(
                name: "headline_en",
                table: "site_profiles");

            migrationBuilder.DropColumn(
                name: "body_en",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "summary_en",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "title_en",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "description_en",
                table: "experiences");

            migrationBuilder.DropColumn(
                name: "title_en",
                table: "experiences");

            migrationBuilder.DropColumn(
                name: "name_en",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "language",
                table: "blog_posts");
        }
    }
}
