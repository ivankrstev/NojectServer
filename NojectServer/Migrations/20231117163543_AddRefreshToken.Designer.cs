﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NojectServer.Data;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NojectServer.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20231117163543_AddRefreshToken")]
    partial class AddRefreshToken
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("NojectServer.Models.RefreshToken", b =>
                {
                    b.Property<string>("Email")
                        .HasMaxLength(62)
                        .HasColumnType("character varying(62)")
                        .HasColumnName("user_id");

                    b.Property<string>("Token")
                        .HasColumnType("text")
                        .HasColumnName("token");

                    b.Property<DateTime>("ExpireDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("valid_until");

                    b.HasKey("Email", "Token");

                    b.ToTable("refresh_tokens");
                });

            modelBuilder.Entity("NojectServer.Models.User", b =>
                {
                    b.Property<string>("Email")
                        .HasMaxLength(62)
                        .HasColumnType("character varying(62)")
                        .HasColumnName("user_id");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("full_name");

                    b.Property<byte[]>("Password")
                        .IsRequired()
                        .HasColumnType("bytea")
                        .HasColumnName("password");

                    b.Property<string>("PasswordResetToken")
                        .HasColumnType("char(128)")
                        .HasColumnName("password_reset_token");

                    b.Property<byte[]>("PasswordSalt")
                        .IsRequired()
                        .HasColumnType("bytea")
                        .HasColumnName("password_salt");

                    b.Property<DateTime?>("ResetTokenExpires")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("reset_token_expires");

                    b.Property<string>("VerificationToken")
                        .HasColumnType("char(128)")
                        .HasColumnName("verification_token");

                    b.Property<DateTime?>("VerifiedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("verified_at");

                    b.HasKey("Email");

                    b.ToTable("users");
                });
#pragma warning restore 612, 618
        }
    }
}
