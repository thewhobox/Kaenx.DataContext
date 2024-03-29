﻿// <auto-generated />
using System;
using Kaenx.DataContext.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kaenx.DataContext.Migrations.Catalog
{
    [DbContext(typeof(CatalogContext))]
    [Migration("20211128131744_Init")]
    partial class Init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.5");

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppAdditional", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ApplicationId")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Assignments")
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("Bindings")
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("ComsAll")
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("ComsDefault")
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("LoadProcedures")
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("ParamsHelper")
                        .HasColumnType("BLOB");

                    b.HasKey("Id");

                    b.ToTable("AppAdditionals");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppComObject", b =>
                {
                    b.Property<int>("UId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ApplicationId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Datapoint")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DatapointSub")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Flag_Communicate")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Flag_Read")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Flag_ReadOnInit")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Flag_Transmit")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Flag_Update")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Flag_Write")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FunctionText")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<string>("Group")
                        .HasColumnType("TEXT");

                    b.Property<int>("Id")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Number")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Size")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.HasKey("UId");

                    b.ToTable("AppComObjects");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppParameter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Access")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ApplicationId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Hash")
                        .HasColumnType("TEXT")
                        .HasMaxLength(40);

                    b.Property<int>("Offset")
                        .HasColumnType("INTEGER");

                    b.Property<int>("OffsetBit")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ParameterId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ParameterTypeId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SegmentId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SegmentType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SuffixText")
                        .HasColumnType("TEXT")
                        .HasMaxLength(20);

                    b.Property<string>("Text")
                        .HasColumnType("TEXT");

                    b.Property<bool>("UnionDefault")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UnionId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("AppParameters");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppParameterTypeEnumViewModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ParameterId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<int>("TypeId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.HasKey("Id");

                    b.ToTable("AppParameterTypeEnums");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppParameterTypeViewModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ApplicationId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasMaxLength(70);

                    b.Property<int>("Size")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Tag1")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<string>("Tag2")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("AppParameterTypes");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.AppSegmentViewModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Address")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ApplicationId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Data")
                        .HasColumnType("TEXT");

                    b.Property<int>("LsmId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Mask")
                        .HasColumnType("TEXT");

                    b.Property<int>("Offset")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SegmentId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Size")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("AppSegments");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.ApplicationViewModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("HardwareId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Hash")
                        .HasColumnType("TEXT")
                        .HasMaxLength(40);

                    b.Property<bool>("IsRelativeSegment")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LoadProcedure")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Manufacturer")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Mask")
                        .HasColumnType("TEXT")
                        .HasMaxLength(7);

                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<int>("Number")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Table_Assosiations")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Table_Assosiations_Max")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Table_Assosiations_Offset")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Table_Group")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Table_Group_Max")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Table_Group_Offset")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Table_Object")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Table_Object_Offset")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Version")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.CatalogViewModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ImportType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Key")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<int>("ParentId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Sections");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.DeviceViewModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("BusCurrent")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CatalogId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("HardwareId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("HasApplicationProgram")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("HasIndividualAddress")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ImportType")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsCoupler")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsPowerSupply")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsRailMounted")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Key")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<int>("ManufacturerId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<string>("OrderNumber")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<string>("VisibleDescription")
                        .HasColumnType("TEXT")
                        .HasMaxLength(300);

                    b.HasKey("Id");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.Hardware2AppModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ManuId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.Property<string>("Number")
                        .HasColumnType("TEXT")
                        .HasMaxLength(30);

                    b.Property<int>("Version")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Hardware2App");
                });

            modelBuilder.Entity("Kaenx.DataContext.Catalog.ManufacturerViewModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ImportType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ManuId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasMaxLength(100);

                    b.HasKey("Id");

                    b.ToTable("Manufacturers");
                });
#pragma warning restore 612, 618
        }
    }
}
