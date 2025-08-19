using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TerminBooking.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeServiceOptional_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Pretvori stare nule u NULL (ako ih ima)
            migrationBuilder.Sql(@"
UPDATE A SET ServiceId = NULL
FROM Appointments A
WHERE A.ServiceId = 0;
");

            // 2) Ukloni DEFAULT constraint nad Appointments.ServiceId (ako postoji)
            migrationBuilder.Sql(@"
DECLARE @dfName sysname;
SELECT @dfName = dc.name
FROM sys.default_constraints AS dc
JOIN sys.columns AS c 
    ON c.default_object_id = dc.object_id 
   AND c.object_id = dc.parent_object_id
JOIN sys.tables  AS t 
    ON t.object_id = c.object_id
WHERE t.name = 'Appointments' AND c.name = 'ServiceId';

IF @dfName IS NOT NULL
BEGIN
    DECLARE @sql nvarchar(max) =
        N'ALTER TABLE [dbo].[Appointments] DROP CONSTRAINT [' + @dfName + N']';
    EXEC sp_executesql @sql;
END
");

            // 3) Postavi kolonu na NULLable
            migrationBuilder.AlterColumn<int>(
                name: "ServiceId",
                table: "Appointments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Vrati NULL-ove na 0 prije nego što opet učinimo NOT NULL
            migrationBuilder.Sql(@"
UPDATE A SET ServiceId = 0
FROM Appointments A
WHERE A.ServiceId IS NULL;
");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceId",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
