﻿// <auto-generated />
using System;
using Kaenx.DataContext.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kaenx.DataContext.Migrations.Local
{
    [DbContext(typeof(LocalContext))]
    [Migration("20200311080217_Init")]
    partial class Init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("Kaenx.DataContext.Local.LocalConnection", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DbHostname");

                    b.Property<string>("DbName");

                    b.Property<string>("DbPassword");

                    b.Property<string>("DbUsername");

                    b.Property<string>("Name");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.ToTable("LocalConnection");
                });

            modelBuilder.Entity("Kaenx.DataContext.Local.LocalProject", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("ConnectionId");

                    b.Property<string>("Name");

                    b.Property<byte[]>("Thumbnail");

                    b.HasKey("Id");

                    b.ToTable("Projects");
                });
#pragma warning restore 612, 618
        }
    }
}
