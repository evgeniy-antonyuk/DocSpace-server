// <auto-generated />
using System;
using ASC.Core.Common.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ASC.Migrations.PostgreSql.Migrations
{
    [DbContext(typeof(CoreDbContext))]
    partial class CoreDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "6.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("ASC.Core.Common.EF.DbButton", b =>
                {
                    b.Property<int>("TariffId")
                        .HasColumnType("integer")
                        .HasColumnName("tariff_id");

                    b.Property<string>("PartnerId")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("partner_id");

                    b.Property<string>("ButtonUrl")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("button_url");

                    b.HasKey("TariffId", "PartnerId")
                        .HasName("tenants_buttons_pkey");

                    b.ToTable("tenants_buttons", "onlyoffice");
                });

            modelBuilder.Entity("ASC.Core.Common.EF.DbQuota", b =>
                {
                    b.Property<int>("Tenant")
                        .HasColumnType("integer")
                        .HasColumnName("tenant");

                    b.Property<int>("ActiveUsers")
                        .HasColumnType("integer")
                        .HasColumnName("active_users");

                    b.Property<string>("AvangateId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("avangate_id")
                        .HasDefaultValueSql("NULL");

                    b.Property<string>("Description")
                        .HasColumnType("character varying")
                        .HasColumnName("description");

                    b.Property<string>("Features")
                        .HasColumnType("text")
                        .HasColumnName("features");

                    b.Property<long>("MaxFileSize")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("max_file_size")
                        .HasDefaultValueSql("'0'");

                    b.Property<long>("MaxTotalSize")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("max_total_size")
                        .HasDefaultValueSql("'0'");

                    b.Property<string>("Name")
                        .HasColumnType("character varying")
                        .HasColumnName("name");

                    b.Property<decimal>("Price")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(10,2)")
                        .HasColumnName("price")
                        .HasDefaultValueSql("0.00");

                    b.Property<bool>("Visible")
                        .HasColumnType("boolean")
                        .HasColumnName("visible");

                    b.HasKey("Tenant")
                        .HasName("tenants_quota_pkey");

                    b.ToTable("tenants_quota", "onlyoffice");

                    b.HasData(
                        new
                        {
                            Tenant = -1,
                            ActiveUsers = 10000,
                            AvangateId = "0",
                            Features = "domain,audit,controlpanel,healthcheck,ldap,sso,whitelabel,branding,ssbranding,update,support,portals:10000,discencryption,privacyroom,restore",
                            MaxFileSize = 102400L,
                            MaxTotalSize = 10995116277760L,
                            Name = "default",
                            Price = 0m,
                            Visible = false
                        });
                });

            modelBuilder.Entity("ASC.Core.Common.EF.DbQuotaRow", b =>
                {
                    b.Property<int>("Tenant")
                        .HasColumnType("integer")
                        .HasColumnName("tenant");

                    b.Property<string>("Path")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("path");

                    b.Property<long>("Counter")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("counter")
                        .HasDefaultValueSql("'0'");

                    b.Property<DateTime>("LastModified")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_modified")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("Tag")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)")
                        .HasColumnName("tag")
                        .HasDefaultValueSql("'0'");

                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(36)
                        .HasColumnType("uuid")
                        .HasColumnName("user_id")
                        .HasDefaultValueSql("NULL");

                    b.HasKey("Tenant", "Path")
                        .HasName("tenants_quotarow_pkey");

                    b.HasIndex("LastModified")
                        .HasDatabaseName("last_modified_tenants_quotarow");

                    b.ToTable("tenants_quotarow", "onlyoffice");
                });

            modelBuilder.Entity("ASC.Core.Common.EF.DbTariff", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Comment")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("comment")
                        .HasDefaultValueSql("NULL");

                    b.Property<DateTime>("CreateOn")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("create_on")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<int>("Quantity")
                        .HasColumnType("integer")
                        .HasColumnName("quantity");

                    b.Property<DateTime>("Stamp")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("stamp");

                    b.Property<int>("Tariff")
                        .HasColumnType("integer")
                        .HasColumnName("tariff");

                    b.Property<int>("Tenant")
                        .HasColumnType("integer")
                        .HasColumnName("tenant");

                    b.HasKey("Id");

                    b.HasIndex("Tenant")
                        .HasDatabaseName("tenant_tenants_tariff");

                    b.ToTable("tenants_tariff", "onlyoffice");
                });
#pragma warning restore 612, 618
        }
    }
}
