﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using VRAtlas;

#nullable disable

namespace VRAtlas.Migrations
{
    [DbContext(typeof(AtlasContext))]
    partial class AtlasContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("VRAtlas.Models.Event", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Instant?>("EndTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("Media")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("OwnerId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("RSVPId")
                        .HasColumnType("uuid");

                    b.Property<Instant?>("StartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.HasIndex("OwnerId");

                    b.HasIndex("RSVPId");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("VRAtlas.Models.EventStar", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Guid?>("EventId")
                        .HasColumnType("uuid");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("EventId");

                    b.HasIndex("UserId");

                    b.ToTable("EventStar");
                });

            modelBuilder.Entity("VRAtlas.Models.EventTag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Guid>("EventId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("TagId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("EventId");

                    b.HasIndex("TagId");

                    b.ToTable("EventTags");
                });

            modelBuilder.Entity("VRAtlas.Models.Follow", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Guid>("EntityId")
                        .HasColumnType("uuid");

                    b.Property<int>("EntityType")
                        .HasColumnType("integer");

                    b.Property<Instant>("FollowedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("MetadataId")
                        .HasColumnType("integer");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("MetadataId");

                    b.HasIndex("UserId");

                    b.ToTable("Follows");
                });

            modelBuilder.Entity("VRAtlas.Models.Group", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("Banner")
                        .HasColumnType("uuid");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("Icon")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("Groups");
                });

            modelBuilder.Entity("VRAtlas.Models.GroupMember", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Guid>("GroupId")
                        .HasColumnType("uuid");

                    b.Property<Instant>("JoinedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("GroupId");

                    b.HasIndex("UserId");

                    b.ToTable("GroupMembers");
                });

            modelBuilder.Entity("VRAtlas.Models.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("EntityId")
                        .HasColumnType("uuid");

                    b.Property<int?>("EntityType")
                        .HasColumnType("integer");

                    b.Property<bool>("Read")
                        .HasColumnType("boolean");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("VRAtlas.Models.NotificationMetadata", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("AtOneDay")
                        .HasColumnType("boolean");

                    b.Property<bool>("AtOneHour")
                        .HasColumnType("boolean");

                    b.Property<bool>("AtStart")
                        .HasColumnType("boolean");

                    b.Property<bool>("AtThirtyMinutes")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.ToTable("NotificationMetadata");
                });

            modelBuilder.Entity("VRAtlas.Models.RSVP", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int?>("Capacity")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("RSVP");
                });

            modelBuilder.Entity("VRAtlas.Models.Tag", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Instant>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("CreatedById")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("CreatedById");

                    b.HasIndex("Id");

                    b.HasIndex("Name");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("VRAtlas.Models.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Biography")
                        .HasColumnType("text");

                    b.Property<int>("DefaultNotificationSettingsId")
                        .HasColumnType("integer");

                    b.Property<Instant>("JoinedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Instant>("LastLoginAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Links")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("MetadataId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("Picture")
                        .HasColumnType("uuid");

                    b.Property<string>("SocialId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("DefaultNotificationSettingsId");

                    b.HasIndex("Id");

                    b.HasIndex("MetadataId");

                    b.HasIndex("SocialId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("VRAtlas.Models.UserMetadata", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("CurrentSocialPlatformProfilePicture")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("CurrentSocialPlatformUsername")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("SynchronizeProfilePictureWithSocialPlatform")
                        .HasColumnType("boolean");

                    b.Property<bool>("SynchronizeUsernameWithSocialPlatform")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.ToTable("UserMetadata");
                });

            modelBuilder.Entity("VRAtlas.Models.Event", b =>
                {
                    b.HasOne("VRAtlas.Models.Group", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("VRAtlas.Models.RSVP", "RSVP")
                        .WithMany()
                        .HasForeignKey("RSVPId");

                    b.Navigation("Owner");

                    b.Navigation("RSVP");
                });

            modelBuilder.Entity("VRAtlas.Models.EventStar", b =>
                {
                    b.HasOne("VRAtlas.Models.Event", null)
                        .WithMany("Stars")
                        .HasForeignKey("EventId");

                    b.HasOne("VRAtlas.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("VRAtlas.Models.EventTag", b =>
                {
                    b.HasOne("VRAtlas.Models.Event", "Event")
                        .WithMany("Tags")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("VRAtlas.Models.Tag", "Tag")
                        .WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("VRAtlas.Models.Follow", b =>
                {
                    b.HasOne("VRAtlas.Models.NotificationMetadata", "Metadata")
                        .WithMany()
                        .HasForeignKey("MetadataId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("VRAtlas.Models.User", null)
                        .WithMany("Following")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Metadata");
                });

            modelBuilder.Entity("VRAtlas.Models.GroupMember", b =>
                {
                    b.HasOne("VRAtlas.Models.Group", "Group")
                        .WithMany("Members")
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("VRAtlas.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Group");

                    b.Navigation("User");
                });

            modelBuilder.Entity("VRAtlas.Models.Notification", b =>
                {
                    b.HasOne("VRAtlas.Models.User", null)
                        .WithMany("Notifications")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("VRAtlas.Models.Tag", b =>
                {
                    b.HasOne("VRAtlas.Models.User", "CreatedBy")
                        .WithMany()
                        .HasForeignKey("CreatedById")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CreatedBy");
                });

            modelBuilder.Entity("VRAtlas.Models.User", b =>
                {
                    b.HasOne("VRAtlas.Models.NotificationMetadata", "DefaultNotificationSettings")
                        .WithMany()
                        .HasForeignKey("DefaultNotificationSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("VRAtlas.Models.UserMetadata", "Metadata")
                        .WithMany()
                        .HasForeignKey("MetadataId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DefaultNotificationSettings");

                    b.Navigation("Metadata");
                });

            modelBuilder.Entity("VRAtlas.Models.Event", b =>
                {
                    b.Navigation("Stars");

                    b.Navigation("Tags");
                });

            modelBuilder.Entity("VRAtlas.Models.Group", b =>
                {
                    b.Navigation("Members");
                });

            modelBuilder.Entity("VRAtlas.Models.User", b =>
                {
                    b.Navigation("Following");

                    b.Navigation("Notifications");
                });
#pragma warning restore 612, 618
        }
    }
}
