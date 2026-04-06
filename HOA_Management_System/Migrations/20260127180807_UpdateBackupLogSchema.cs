using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOA_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBackupLogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Property",
                columns: table => new
                {
                    Property_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Block = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Lot = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Date_Registered = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Property__2757E6F68939D922", x => x.Property_ID);
                });

            migrationBuilder.CreateTable(
                name: "Membership",
                columns: table => new
                {
                    Membership_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Property_ID = table.Column<int>(type: "int", nullable: false),
                    Last_Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    First_Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Middle_Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Civil_Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Sex = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    Date_of_Birth = table.Column<DateOnly>(type: "date", nullable: true),
                    Spouse_Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Contact_Number = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    Email_Address = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Years_Stay = table.Column<int>(type: "int", nullable: true),
                    Is_Voter = table.Column<bool>(type: "bit", nullable: false),
                    Preferred_Payment = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Count_Seniors = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Count_Helpers = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Count_Children = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Count_Others = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Total_Members = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Count_Dogs = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Is_Dog_Leashed = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    Count_Cats = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Is_Cat_Leashed = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    Other_Pets = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Membersh__0C6DBA47148B60F3", x => x.Membership_ID);
                    table.ForeignKey(
                        name: "FK_Membership_Property",
                        column: x => x.Property_ID,
                        principalTable: "Property",
                        principalColumn: "Property_ID");
                });

            migrationBuilder.CreateTable(
                name: "MonthlyDues",
                columns: table => new
                {
                    Bill_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Property_ID = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Amount_Due = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Total_Paid = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    Date_Fully_Paid = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Unpaid")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MonthlyD__CF6E7D435F40EE71", x => x.Bill_ID);
                    table.ForeignKey(
                        name: "FK_Dues_Property",
                        column: x => x.Property_ID,
                        principalTable: "Property",
                        principalColumn: "Property_ID");
                });

            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    Tenant_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Property_ID = table.Column<int>(type: "int", nullable: false),
                    Full_Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Contact_Number = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Active"),
                    Start_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    End_Date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Tenant__8E8F34722302DA74", x => x.Tenant_ID);
                    table.ForeignKey(
                        name: "FK_Tenant_Property",
                        column: x => x.Property_ID,
                        principalTable: "Property",
                        principalColumn: "Property_ID");
                });

            migrationBuilder.CreateTable(
                name: "Vehicle",
                columns: table => new
                {
                    Vehicle_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Property_ID = table.Column<int>(type: "int", nullable: false),
                    Vehicle_Owner = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Driver_Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Vehicle_Type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Make = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Model = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Year = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true),
                    Color = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Plate_Number = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    Sticker_Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Sticker_Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Vehicle__CE6D7CB5BC31589B", x => x.Vehicle_ID);
                    table.ForeignKey(
                        name: "FK_Vehicle_Property",
                        column: x => x.Property_ID,
                        principalTable: "Property",
                        principalColumn: "Property_ID");
                });

            migrationBuilder.CreateTable(
                name: "UserAccount",
                columns: table => new
                {
                    User_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Membership_ID = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Password_Hash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Is_Active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserAcco__206D9190013513EC", x => x.User_ID);
                    table.ForeignKey(
                        name: "FK_User_Membership",
                        column: x => x.Membership_ID,
                        principalTable: "Membership",
                        principalColumn: "Membership_ID");
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Audit_ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action_Type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Table_Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Record_ID = table.Column<int>(type: "int", nullable: false),
                    User_ID = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AuditLog__EDBEC759741366CD", x => x.Audit_ID);
                    table.ForeignKey(
                        name: "FK_Audit_User",
                        column: x => x.User_ID,
                        principalTable: "UserAccount",
                        principalColumn: "User_ID");
                });

            migrationBuilder.CreateTable(
                name: "BackupLog",
                columns: table => new
                {
                    Backup_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Backup_File_Name = table.Column<string>(type: "varchar(255)", nullable: false),
                    Backup_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Performed_By = table.Column<int>(type: "int", nullable: false),
                    Backup_Type = table.Column<string>(type: "varchar(20)", nullable: false),
                    Notes = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupLog", x => x.Backup_ID);
                    table.ForeignKey(
                        name: "FK_BackupLog_UserAccount_Performed_By",
                        column: x => x.Performed_By,
                        principalTable: "UserAccount",
                        principalColumn: "User_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Certification",
                columns: table => new
                {
                    Certification_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Property_ID = table.Column<int>(type: "int", nullable: false),
                    Requestor_Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Cert_Type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Date_Requested = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(getdate())"),
                    Date_Issued = table.Column<DateOnly>(type: "date", nullable: true),
                    Issued_By = table.Column<int>(type: "int", nullable: true),
                    Fee_Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Certific__D9E41EB8CA1CDA20", x => x.Certification_ID);
                    table.ForeignKey(
                        name: "FK_Cert_Property",
                        column: x => x.Property_ID,
                        principalTable: "Property",
                        principalColumn: "Property_ID");
                    table.ForeignKey(
                        name: "FK_Cert_User",
                        column: x => x.Issued_By,
                        principalTable: "UserAccount",
                        principalColumn: "User_ID");
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    Transaction_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Property_ID = table.Column<int>(type: "int", nullable: false),
                    OR_Number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Payment_Date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(getdate())"),
                    Payment_Type = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Encoded_By = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Transact__9A8D5625EEFB2B84", x => x.Transaction_ID);
                    table.ForeignKey(
                        name: "FK_Transaction_Property",
                        column: x => x.Property_ID,
                        principalTable: "Property",
                        principalColumn: "Property_ID");
                    table.ForeignKey(
                        name: "FK_Transaction_User",
                        column: x => x.Encoded_By,
                        principalTable: "UserAccount",
                        principalColumn: "User_ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_User_ID",
                table: "AuditLog",
                column: "User_ID");

            migrationBuilder.CreateIndex(
                name: "IX_BackupLog_Performed_By",
                table: "BackupLog",
                column: "Performed_By");

            migrationBuilder.CreateIndex(
                name: "IX_Certification_Issued_By",
                table: "Certification",
                column: "Issued_By");

            migrationBuilder.CreateIndex(
                name: "IX_Certification_Property_ID",
                table: "Certification",
                column: "Property_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Membership_Property_ID",
                table: "Membership",
                column: "Property_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyDues_Property_ID",
                table: "MonthlyDues",
                column: "Property_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_Property_ID",
                table: "Tenant",
                column: "Property_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_Encoded_By",
                table: "Transaction",
                column: "Encoded_By");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_Property_ID",
                table: "Transaction",
                column: "Property_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__Transact__1F95B8BB52E32931",
                table: "Transaction",
                column: "OR_Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_Membership_ID",
                table: "UserAccount",
                column: "Membership_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__UserAcco__536C85E41F1083E2",
                table: "UserAccount",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_Property_ID",
                table: "Vehicle",
                column: "Property_ID");

            migrationBuilder.CreateIndex(
                name: "UQ__Vehicle__5834C125E913FB8F",
                table: "Vehicle",
                column: "Plate_Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "BackupLog");

            migrationBuilder.DropTable(
                name: "Certification");

            migrationBuilder.DropTable(
                name: "MonthlyDues");

            migrationBuilder.DropTable(
                name: "Tenant");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "Vehicle");

            migrationBuilder.DropTable(
                name: "UserAccount");

            migrationBuilder.DropTable(
                name: "Membership");

            migrationBuilder.DropTable(
                name: "Property");
        }
    }
}
