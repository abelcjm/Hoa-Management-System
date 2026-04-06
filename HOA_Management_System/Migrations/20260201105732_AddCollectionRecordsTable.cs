using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOA_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionRecordsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BackupLog_UserAccount_Performed_By",
                table: "BackupLog");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Membership",
                table: "UserAccount");

            migrationBuilder.DropColumn(
                name: "Date_Requested",
                table: "Certification");

            migrationBuilder.RenameColumn(
                name: "Membership_ID",
                table: "UserAccount",
                newName: "PropertyId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAccount_Membership_ID",
                table: "UserAccount",
                newName: "IX_UserAccount_PropertyId");

            migrationBuilder.AlterColumn<string>(
                name: "Password_Hash",
                table: "UserAccount",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldUnicode: false,
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "UserAccount",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Last_Login",
                table: "UserAccount",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tenant",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Start_Date",
                table: "Tenant",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "Full_Name",
                table: "Tenant",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "End_Date",
                table: "Tenant",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Contact_Number",
                table: "Tenant",
                type: "varchar(15)",
                unicode: false,
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(15)",
                oldUnicode: false,
                oldMaxLength: 15);

            migrationBuilder.AlterColumn<int>(
                name: "Performed_By",
                table: "BackupLog",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "CollectionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    TotalTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCollected = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EfficiencyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionRecords", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_BackupLog_UserAccount_Performed_By",
                table: "BackupLog",
                column: "Performed_By",
                principalTable: "UserAccount",
                principalColumn: "User_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_User_Property",
                table: "UserAccount",
                column: "PropertyId",
                principalTable: "Property",
                principalColumn: "Property_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BackupLog_UserAccount_Performed_By",
                table: "BackupLog");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Property",
                table: "UserAccount");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "CollectionRecords");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "UserAccount");

            migrationBuilder.DropColumn(
                name: "Last_Login",
                table: "UserAccount");

            migrationBuilder.RenameColumn(
                name: "PropertyId",
                table: "UserAccount",
                newName: "Membership_ID");

            migrationBuilder.RenameIndex(
                name: "IX_UserAccount_PropertyId",
                table: "UserAccount",
                newName: "IX_UserAccount_Membership_ID");

            migrationBuilder.AlterColumn<string>(
                name: "Password_Hash",
                table: "UserAccount",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldUnicode: false,
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Tenant",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldNullable: true,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Start_Date",
                table: "Tenant",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Full_Name",
                table: "Tenant",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "End_Date",
                table: "Tenant",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Contact_Number",
                table: "Tenant",
                type: "varchar(15)",
                unicode: false,
                maxLength: 15,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(15)",
                oldUnicode: false,
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date_Requested",
                table: "Certification",
                type: "date",
                nullable: false,
                defaultValueSql: "(getdate())");

            migrationBuilder.AlterColumn<int>(
                name: "Performed_By",
                table: "BackupLog",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BackupLog_UserAccount_Performed_By",
                table: "BackupLog",
                column: "Performed_By",
                principalTable: "UserAccount",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Membership",
                table: "UserAccount",
                column: "Membership_ID",
                principalTable: "Membership",
                principalColumn: "Membership_ID");
        }
    }
}
