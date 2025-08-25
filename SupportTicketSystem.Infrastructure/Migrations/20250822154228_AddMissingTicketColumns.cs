using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupportTicketSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTicketColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentMethod",
                table: "Tickets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentReason",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessImpactData",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerFeedback",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerSatisfactionScore",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EscalatedAt",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscalatedById",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationReason",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstResponseAt",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstResponseDeadline",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEscalated",
                table: "Tickets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolutionDeadline",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_EscalatedById",
                table: "Tickets",
                column: "EscalatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_EscalatedById",
                table: "Tickets",
                column: "EscalatedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_EscalatedById",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_EscalatedById",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AssignmentMethod",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AssignmentReason",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "BusinessImpactData",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CustomerFeedback",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CustomerSatisfactionScore",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EscalatedAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EscalatedById",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EscalationReason",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FirstResponseAt",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "FirstResponseDeadline",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "IsEscalated",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ResolutionDeadline",
                table: "Tickets");
        }
    }
}
