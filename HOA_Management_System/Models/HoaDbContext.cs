    using System;
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore;

    namespace HOA_Management_System.Models;

    public partial class HoaDbContext : DbContext
    {
        public HoaDbContext()
        {
        }

        public HoaDbContext(DbContextOptions<HoaDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AuditLog> AuditLogs { get; set; }

        public virtual DbSet<Certification> Certifications { get; set; }

        public virtual DbSet<Membership> Memberships { get; set; }

        public virtual DbSet<MonthlyDue> MonthlyDues { get; set; }

        public virtual DbSet<Property> Properties { get; set; }

        public virtual DbSet<Tenant> Tenants { get; set; }

        public virtual DbSet<Transaction> Transactions { get; set; }

        public virtual DbSet<UserAccount> UserAccounts { get; set; }

        public virtual DbSet<Vehicle> Vehicles { get; set; }

        public virtual DbSet<BackupLog> BackupLogs { get; set; }

        public virtual DbSet<AppSetting> AppSettings { get; set; }
        public virtual DbSet<CollectionRecord> CollectionRecords { get; set; }
        public virtual DbSet<DelinquencyRecord> DelinquencyRecords { get; set; }
        public virtual DbSet<DelinquencyHistory> DelinquencyHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.AuditId).HasName("PK__AuditLog__EDBEC759741366CD");

                entity.ToTable("AuditLog");

                entity.Property(e => e.AuditId).HasColumnName("Audit_ID");
                entity.Property(e => e.ActionType)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("Action_Type");
                entity.Property(e => e.RecordId).HasColumnName("Record_ID");
                entity.Property(e => e.TableName)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("Table_Name");
                entity.Property(e => e.Timestamp)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.UserId).HasColumnName("User_ID");

                entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Audit_User");
            });

            modelBuilder.Entity<Certification>(entity =>
            {
                entity.HasKey(e => e.CertificationId).HasName("PK__Certific__D9E41EB8CA1CDA20");

                entity.ToTable("Certification");

                entity.Property(e => e.CertificationId).HasColumnName("Certification_ID");
                entity.Property(e => e.CertType)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("Cert_Type");
                entity.Property(e => e.DateIssued).HasColumnName("Date_Issued");
                entity.Property(e => e.FeeAmount)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("Fee_Amount");
                entity.Property(e => e.IssuedBy).HasColumnName("Issued_By");
                entity.Property(e => e.PropertyId).HasColumnName("Property_ID");
                entity.Property(e => e.RequestorName)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("Requestor_Name");

                entity.HasOne(d => d.IssuedByNavigation).WithMany(p => p.Certifications)
                    .HasForeignKey(d => d.IssuedBy)
                    .HasConstraintName("FK_Cert_User");

                entity.HasOne(d => d.Property).WithMany(p => p.Certifications)
                    .HasForeignKey(d => d.PropertyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Cert_Property");
            });

            modelBuilder.Entity<Membership>(entity =>
            {
                entity.HasKey(e => e.MembershipId).HasName("PK__Membersh__0C6DBA47148B60F3");

                entity.ToTable("Membership");

                entity.Property(e => e.MembershipId).HasColumnName("Membership_ID");
                entity.Property(e => e.CivilStatus)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("Civil_Status");
                entity.Property(e => e.ContactNumber)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("Contact_Number");
                entity.Property(e => e.CountCats)
                    .HasDefaultValue(0)
                    .HasColumnName("Count_Cats");
                entity.Property(e => e.CountChildren)
                    .HasDefaultValue(0)
                    .HasColumnName("Count_Children");
                entity.Property(e => e.CountDogs)
                    .HasDefaultValue(0)
                    .HasColumnName("Count_Dogs");
                entity.Property(e => e.CountHelpers)
                    .HasDefaultValue(0)
                    .HasColumnName("Count_Helpers");
                entity.Property(e => e.CountOthers)
                    .HasDefaultValue(0)
                    .HasColumnName("Count_Others");
                entity.Property(e => e.CountSeniors)
                    .HasDefaultValue(0)
                    .HasColumnName("Count_Seniors");
                entity.Property(e => e.DateOfBirth).HasColumnName("Date_of_Birth");
                entity.Property(e => e.EmailAddress)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("Email_Address");
                entity.Property(e => e.FirstName)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("First_Name");
                entity.Property(e => e.IsCatLeashed)
                    .HasDefaultValue(false)
                    .HasColumnName("Is_Cat_Leashed");
                entity.Property(e => e.IsDogLeashed)
                    .HasDefaultValue(false)
                    .HasColumnName("Is_Dog_Leashed");
                entity.Property(e => e.IsVoter).HasColumnName("Is_Voter");
                entity.Property(e => e.LastName)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("Last_Name");
                entity.Property(e => e.MiddleName)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("Middle_Name");
                entity.Property(e => e.OtherPets)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("Other_Pets");
                entity.Property(e => e.PreferredPayment)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("Preferred_Payment");
                entity.Property(e => e.PropertyId).HasColumnName("Property_ID");
                entity.Property(e => e.Sex)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.SpouseName)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("Spouse_Name");
                entity.Property(e => e.TotalMembers)
                    .HasDefaultValue(0)
                    .HasColumnName("Total_Members");
                entity.Property(e => e.YearsStay).HasColumnName("Years_Stay");

                entity.HasOne(d => d.Property).WithMany(p => p.Memberships)
                    .HasForeignKey(d => d.PropertyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Membership_Property");
            });

            modelBuilder.Entity<MonthlyDue>(entity =>
            {
                entity.HasKey(e => e.BillId).HasName("PK__MonthlyD__CF6E7D435F40EE71");

                entity.Property(e => e.BillId).HasColumnName("Bill_ID");
                entity.Property(e => e.AmountDue)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("Amount_Due");
                entity.Property(e => e.DateFullyPaid).HasColumnName("Date_Fully_Paid");
                entity.Property(e => e.PropertyId).HasColumnName("Property_ID");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValue("Unpaid");
                entity.Property(e => e.TotalPaid)
                    .HasDefaultValue(0m)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("Total_Paid");

                entity.HasOne(d => d.Property).WithMany(p => p.MonthlyDues)
                    .HasForeignKey(d => d.PropertyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Dues_Property");
            });

            modelBuilder.Entity<Property>(entity =>
            {
                entity.HasKey(e => e.PropertyId).HasName("PK__Property__2757E6F68939D922");

                entity.ToTable("Property");

                entity.Property(e => e.PropertyId).HasColumnName("Property_ID");
                entity.Property(e => e.Block)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.DateRegistered)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnName("Date_Registered");
                entity.Property(e => e.Lot)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.TenantId).HasName("PK__Tenant__8E8F34722302DA74");

                entity.ToTable("Tenant");

                entity.Property(e => e.TenantId).HasColumnName("Tenant_ID");
                entity.Property(e => e.ContactNumber)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("Contact_Number");
                entity.Property(e => e.EndDate).HasColumnName("End_Date");
                entity.Property(e => e.FullName)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("Full_Name");
                entity.Property(e => e.PropertyId).HasColumnName("Property_ID");
                entity.Property(e => e.StartDate).HasColumnName("Start_Date");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasDefaultValue("Active");

                entity.HasOne(d => d.Property).WithMany(p => p.Tenants)
                    .HasForeignKey(d => d.PropertyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Tenant_Property");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.TransactionId).HasName("PK__Transact__9A8D5625EEFB2B84");

                entity.ToTable("Transaction");

                entity.HasIndex(e => e.OrNumber, "UQ__Transact__1F95B8BB52E32931").IsUnique();

                entity.Property(e => e.TransactionId).HasColumnName("Transaction_ID");
                entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.EncodedBy).HasColumnName("Encoded_By");
                entity.Property(e => e.Notes)
                    .HasMaxLength(255)
                    .IsUnicode(false);
                entity.Property(e => e.OrNumber)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("OR_Number");
                entity.Property(e => e.PaymentDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnName("Payment_Date");
                entity.Property(e => e.PaymentType)
                    .HasMaxLength(30)
                    .IsUnicode(false)
                    .HasColumnName("Payment_Type");
                entity.Property(e => e.PropertyId).HasColumnName("Property_ID");

                entity.HasOne(d => d.EncodedByNavigation).WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.EncodedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Transaction_User");

                entity.HasOne(d => d.Property).WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.PropertyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Transaction_Property");
            });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserAcco__206D9190013513EC");
            entity.ToTable("UserAccount");
            entity.HasIndex(e => e.Username, "UQ__UserAcco__536C85E41F1083E2").IsUnique();
            entity.Property(e => e.UserId).HasColumnName("User_ID");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("Is_Active");
            entity.Property(e => e.PropertyId).HasColumnName("PropertyId");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("Password_Hash");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.HasOne(d => d.Property)
                .WithMany(p => p.UserAccounts)
                .HasForeignKey(d => d.PropertyId)
                .HasConstraintName("FK_User_Property");
        });

        modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.VehicleId).HasName("PK__Vehicle__CE6D7CB5BC31589B");

                entity.ToTable("Vehicle");

                entity.HasIndex(e => e.PlateNumber, "UQ__Vehicle__5834C125E913FB8F").IsUnique();

                entity.Property(e => e.VehicleId).HasColumnName("Vehicle_ID");
                entity.Property(e => e.Color)
                    .HasMaxLength(20)
                    .IsUnicode(false);
                entity.Property(e => e.DriverName)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("Driver_Name");
                entity.Property(e => e.Make)
                    .HasMaxLength(30)
                    .IsUnicode(false);
                entity.Property(e => e.Model)
                    .HasMaxLength(30)
                    .IsUnicode(false);
                entity.Property(e => e.PlateNumber)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("Plate_Number");
                entity.Property(e => e.PropertyId).HasColumnName("Property_ID");
                entity.Property(e => e.StickerNumber)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("Sticker_Number");
                entity.Property(e => e.StickerYear).HasColumnName("Sticker_Year");
                entity.Property(e => e.VehicleOwner)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("Vehicle_Owner");
                entity.Property(e => e.VehicleType)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("Vehicle_Type");
                entity.Property(e => e.Year)
                    .HasMaxLength(4)
                    .IsUnicode(false);

                entity.HasOne(d => d.Property).WithMany(p => p.Vehicles)
                    .HasForeignKey(d => d.PropertyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vehicle_Property");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
