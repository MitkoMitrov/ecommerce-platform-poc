using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Commerce.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchased_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchases", x => x.id);
                    table.CheckConstraint("ck_purchases_total_non_negative", "\"total\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "purchase_items",
                columns: table => new
                {
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_items", x => new { x.purchase_id, x.product_id });
                    table.CheckConstraint("ck_purchase_items_quantity_range", "\"quantity\" >= 1 AND \"quantity\" <= 99");
                    table.CheckConstraint("ck_purchase_items_unit_price_non_negative", "\"unit_price\" >= 0");
                    table.ForeignKey(
                        name: "fk_purchase_items_purchases_purchase_id",
                        column: x => x.purchase_id,
                        principalTable: "purchases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_items_product_id",
                table: "purchase_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchases_purchased_at_utc_id",
                table: "purchases",
                columns: new[] { "purchased_at_utc", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_items");

            migrationBuilder.DropTable(
                name: "purchases");
        }
    }
}
