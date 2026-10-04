using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace planno_API.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    start_time = table.Column<DateTime>(type: "datetime", nullable: false),
                    end_time = table.Column<DateTime>(type: "datetime", nullable: false),
                    location = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    is_all_day = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    interval = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    active_subscription_id = table.Column<int>(type: "int", nullable: true),
                    subscription_expires_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_subscriptions_active_subscription_id",
                        column: x => x.active_subscription_id,
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "card_details",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    card_brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    card_last4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    user_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_card_details_users_user_Id",
                        column: x => x.user_Id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_Id = table.Column<int>(type: "int", nullable: false),
                    appointment_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_appointments_appointments_appointment_Id",
                        column: x => x.appointment_Id,
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_appointments_users_user_Id",
                        column: x => x.user_Id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    card_Id = table.Column<int>(type: "int", nullable: false),
                    subscription_Id = table.Column<int>(type: "int", nullable: false),
                    user_Id = table.Column<int>(type: "int", nullable: false),
                    paid_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payments_card_details_card_Id",
                        column: x => x.card_Id,
                        principalTable: "card_details",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_subscriptions_subscription_Id",
                        column: x => x.subscription_Id,
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_users_user_Id",
                        column: x => x.user_Id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    payment_Id = table.Column<int>(type: "int", nullable: false),
                    invoice_number = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    issued_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    pdf_url = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoices_payments_payment_Id",
                        column: x => x.payment_Id,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "subscriptions",
                columns: new[] { "Id", "description", "interval", "price" },
                values: new object[,]
                {
                    { 1, "Premium Monatlich", 1, 2.99m },
                    { 2, "Premium Jährlich", 12, 29.99m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_card_details_user_Id",
                table: "card_details",
                column: "user_Id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_payment_Id",
                table: "invoices",
                column: "payment_Id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_card_Id",
                table: "payments",
                column: "card_Id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_subscription_Id",
                table: "payments",
                column: "subscription_Id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_user_Id",
                table: "payments",
                column: "user_Id");

            migrationBuilder.CreateIndex(
                name: "IX_user_appointments_appointment_Id",
                table: "user_appointments",
                column: "appointment_Id");

            migrationBuilder.CreateIndex(
                name: "IX_user_appointments_user_Id_appointment_Id",
                table: "user_appointments",
                columns: new[] { "user_Id", "appointment_Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_active_subscription_id",
                table: "users",
                column: "active_subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "user_appointments");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "card_details");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "subscriptions");
        }
    }
}
