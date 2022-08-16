// <auto-generated />
using System;
using ASC.Webhooks.Core.EF.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ASC.Migrations.MySql.Migrations
{
    [DbContext(typeof(WebhooksDbContext))]
    partial class WebhooksDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("ASC.Webhooks.Core.EF.Model.WebhooksConfig", b =>
                {
                    b.Property<int>("ConfigId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("config_id");

                    b.Property<string>("SecretKey")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("secret_key")
                        .HasDefaultValueSql("''");

                    b.Property<uint>("TenantId")
                        .HasColumnType("int unsigned")
                        .HasColumnName("tenant_id");

                    b.Property<string>("Uri")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("uri")
                        .HasDefaultValueSql("''");

                    b.HasKey("ConfigId")
                        .HasName("PRIMARY");

                    b.ToTable("webhooks_config", (string)null);

                    b.HasAnnotation("MySql:CharSet", "utf8");
                });

            modelBuilder.Entity("ASC.Webhooks.Core.EF.Model.WebhooksLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    b.Property<int>("ConfigId")
                        .HasColumnType("int")
                        .HasColumnName("config_id");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("datetime")
                        .HasColumnName("creation_time");

                    b.Property<string>("Method")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("method");

                    b.Property<string>("RequestHeaders")
                        .HasColumnType("json")
                        .HasColumnName("request_headers");

                    b.Property<string>("RequestPayload")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("request_payload")
                        .UseCollation("utf8_general_ci")
                        .HasAnnotation("MySql:CharSet", "utf8");

                    b.Property<string>("ResponseHeaders")
                        .HasColumnType("json")
                        .HasColumnName("response_headers");

                    b.Property<string>("ResponsePayload")
                        .HasColumnType("text")
                        .HasColumnName("response_payload")
                        .UseCollation("utf8_general_ci")
                        .HasAnnotation("MySql:CharSet", "utf8");

                    b.Property<string>("Route")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("route");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("status");

                    b.Property<uint>("TenantId")
                        .HasColumnType("int unsigned")
                        .HasColumnName("tenant_id");

                    b.Property<string>("Uid")
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("uid");

                    b.HasKey("Id")
                        .HasName("PRIMARY");

                    b.ToTable("webhooks_logs", (string)null);

                    b.HasAnnotation("MySql:CharSet", "utf8");
                });
#pragma warning restore 612, 618
        }
    }
}
