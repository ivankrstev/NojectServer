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
    [Migration("20231206000337_AddedTaskWithProjectPointers")]
    partial class AddedTaskWithProjectPointers
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("NojectServer.Models.Collaborator", b =>
                {
                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid")
                        .HasColumnName("project_id");

                    b.Property<string>("CollaboratorId")
                        .HasColumnType("character varying(62)")
                        .HasColumnName("user_id");

                    b.HasKey("ProjectId", "CollaboratorId");

                    b.HasIndex("CollaboratorId");

                    b.ToTable("collaborators");
                });

            modelBuilder.Entity("NojectServer.Models.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("project_id");

                    b.Property<string>("BackgroundColor")
                        .IsRequired()
                        .HasColumnType("char(7)")
                        .HasColumnName("background_color");

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasColumnType("char(7)")
                        .HasColumnName("color");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("character varying(62)")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<int?>("FirstTask")
                        .HasColumnType("integer")
                        .HasColumnName("first_task");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("FirstTask");

                    b.ToTable("projects");
                });

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

            modelBuilder.Entity("NojectServer.Models.Task", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("integer")
                        .HasColumnName("task_id");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid")
                        .HasColumnName("project_id");

                    b.Property<bool>("Completed")
                        .HasColumnType("boolean")
                        .HasColumnName("completed");

                    b.Property<string>("CompletedBy")
                        .HasColumnType("character varying(62)")
                        .HasColumnName("completed_by");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("character varying(62)")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("CreatedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_on");

                    b.Property<DateTime?>("LastModifiedOn")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_modified_on");

                    b.Property<int>("Level")
                        .HasColumnType("integer")
                        .HasColumnName("level");

                    b.Property<int?>("Next")
                        .HasColumnType("integer")
                        .HasColumnName("next");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("value");

                    b.HasKey("Id", "ProjectId");

                    b.HasIndex("CompletedBy");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("Next");

                    b.HasIndex("ProjectId");

                    b.ToTable("tasks");
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

            modelBuilder.Entity("NojectServer.Models.Collaborator", b =>
                {
                    b.HasOne("NojectServer.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("CollaboratorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("NojectServer.Models.Project", "Project")
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");

                    b.Navigation("User");
                });

            modelBuilder.Entity("NojectServer.Models.Project", b =>
                {
                    b.HasOne("NojectServer.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("NojectServer.Models.Task", "Task")
                        .WithMany()
                        .HasForeignKey("FirstTask")
                        .HasPrincipalKey("Id")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Task");

                    b.Navigation("User");
                });

            modelBuilder.Entity("NojectServer.Models.RefreshToken", b =>
                {
                    b.HasOne("NojectServer.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("Email")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("NojectServer.Models.Task", b =>
                {
                    b.HasOne("NojectServer.Models.User", "UserWhoCompleted")
                        .WithMany()
                        .HasForeignKey("CompletedBy");

                    b.HasOne("NojectServer.Models.User", "UserWhoCreated")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("NojectServer.Models.Task", "NextTask")
                        .WithMany()
                        .HasForeignKey("Next")
                        .HasPrincipalKey("Id")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("NojectServer.Models.Project", "Project")
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("NextTask");

                    b.Navigation("Project");

                    b.Navigation("UserWhoCompleted");

                    b.Navigation("UserWhoCreated");
                });
#pragma warning restore 612, 618
        }
    }
}
