﻿// <auto-generated />
using System;
using Kaenx.DataContext.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kaenx.DataContext.Migrations.Project
{
    [DbContext(typeof(ProjectContext))]
    [Migration("20210412153437_Deactivate")]
    partial class Deactivate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.5");

            modelBuilder.Entity("Kaenx.DataContext.Project.ChangeParamModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("DeviceId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ParamId")
                        .HasColumnType("TEXT");

                    b.Property<int>("StateId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ChangesParam");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.ComObject", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ComId")
                        .HasColumnType("TEXT");

                    b.Property<int>("DeviceId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Groups")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ComObjects");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.LineDeviceModel", b =>
                {
                    b.Property<int>("UId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ApplicationId")
                        .HasColumnType("TEXT");

                    b.Property<string>("DeviceId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDeactivated")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LastGroupCount")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("LoadedApp")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("LoadedGA")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("LoadedPA")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("ParentId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProjectId")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Serial")
                        .HasColumnType("BLOB");

                    b.HasKey("UId");

                    b.ToTable("LineDevices");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.LineMiddleModel", b =>
                {
                    b.Property<int>("UId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsExpanded")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("ParentId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProjectId")
                        .HasColumnType("INTEGER");

                    b.HasKey("UId");

                    b.ToTable("LinesMiddle");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.LineModel", b =>
                {
                    b.Property<int>("UId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsExpanded")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("ProjectId")
                        .HasColumnType("INTEGER");

                    b.HasKey("UId");

                    b.ToTable("LinesMain");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.ProjectModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Area")
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("Image")
                        .HasColumnType("BLOB");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("Kaenx.DataContext.Project.StateModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT")
                        .HasMaxLength(200);

                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasMaxLength(50);

                    b.Property<int>("ProjectId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("States");
                });
#pragma warning restore 612, 618
        }
    }
}
