﻿// <auto-generated />

namespace ASC.Webhooks.Core.Migrations
{
    [DbContext(typeof(MySqlWebhooksDbContext))]
    partial class WebhooksDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.5");

            modelBuilder.Entity("ASC.Webhooks.Core.Dao.Models.WebhooksConfig", b =>
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

                    b.ToTable("webhooks_config");
                });

            modelBuilder.Entity("ASC.Webhooks.Core.Dao.Models.WebhooksLog", b =>
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

                    b.Property<string>("Event")
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)")
                        .HasColumnName("event");

                    b.Property<string>("RequestHeaders")
                        .HasColumnType("json")
                        .HasColumnName("request_headers");

                    b.Property<string>("RequestPayload")
                        .IsRequired()
                        .HasColumnType("json")
                        .HasColumnName("request_payload");

                    b.Property<string>("ResponseHeaders")
                        .HasColumnType("json")
                        .HasColumnName("response_headers");

                    b.Property<string>("ResponsePayload")
                        .HasColumnType("json")
                        .HasColumnName("response_payload");

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

                    b.ToTable("webhooks_logs");
                });
#pragma warning restore 612, 618
        }
    }
}
