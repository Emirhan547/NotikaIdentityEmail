using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotikaIdentityEmail.Migrations
{
    /// <inheritdoc />
    public partial class mig5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CatgoryIconUrl",
                table: "Categories",
                newName: "CategoryIconUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CategoryIconUrl",
                table: "Categories",
                newName: "CatgoryIconUrl");
        }
    }
}
