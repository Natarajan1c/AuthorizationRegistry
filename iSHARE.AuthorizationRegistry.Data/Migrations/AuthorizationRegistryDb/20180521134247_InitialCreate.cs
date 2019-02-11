﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace iSHARE.AuthorizationRegistry.Data.Migrations.AuthorizationRegistryDb
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Delegations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Delegations", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Delegations");
        }
    }
}
