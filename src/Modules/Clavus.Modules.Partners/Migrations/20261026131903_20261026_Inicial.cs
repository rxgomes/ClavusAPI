using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clavus.Modules.Partners.Migrations
{
    /// <inheritdoc />
    public partial class _20261026_Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "partners");

            migrationBuilder.CreateTable(
                name: "partner",
                schema: "partners",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    trade_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    document = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    municipal_registration = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    state_registration = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    is_client = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_supplier = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    address_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_neighborhood = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    address_state = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    address_state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    address_street = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_street_type = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    address_zip_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    identifier = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    create_in = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "localtimestamp"),
                    update_in = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "localtimestamp"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partner", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contact",
                schema: "partners",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    partner_id = table.Column<long>(type: "bigint", nullable: false),
                    identifier = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    create_in = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "localtimestamp"),
                    update_in = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "localtimestamp"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contact", x => x.id);
                    table.ForeignKey(
                        name: "fk_contact_partner_partner_id",
                        column: x => x.partner_id,
                        principalSchema: "partners",
                        principalTable: "partner",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email",
                schema: "partners",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    address = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: false),
                    type = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    contact_id = table.Column<long>(type: "bigint", nullable: true),
                    partner_id = table.Column<long>(type: "bigint", nullable: true),
                    identifier = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    create_in = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "localtimestamp"),
                    update_in = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "localtimestamp"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email", x => x.id);
                    table.CheckConstraint("ck_email_owner", "num_nonnulls(partner_id, contact_id) = 1");
                    table.ForeignKey(
                        name: "fk_email_contact_contact_id",
                        column: x => x.contact_id,
                        principalSchema: "partners",
                        principalTable: "contact",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_email_partner_partner_id",
                        column: x => x.partner_id,
                        principalSchema: "partners",
                        principalTable: "partner",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phone",
                schema: "partners",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    area_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    number = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    contact_id = table.Column<long>(type: "bigint", nullable: true),
                    partner_id = table.Column<long>(type: "bigint", nullable: true),
                    identifier = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    create_in = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "localtimestamp"),
                    update_in = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "localtimestamp"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phone", x => x.id);
                    table.CheckConstraint("ck_phone_owner", "num_nonnulls(partner_id, contact_id) = 1");
                    table.ForeignKey(
                        name: "fk_phone_contact_contact_id",
                        column: x => x.contact_id,
                        principalSchema: "partners",
                        principalTable: "contact",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_phone_partner_partner_id",
                        column: x => x.partner_id,
                        principalSchema: "partners",
                        principalTable: "partner",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contact_identifier",
                schema: "partners",
                table: "contact",
                column: "identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contact_partner_id",
                schema: "partners",
                table: "contact",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_contact_id",
                schema: "partners",
                table: "email",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_identifier",
                schema: "partners",
                table: "email",
                column: "identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_partner_id",
                schema: "partners",
                table: "email",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_partner_document",
                schema: "partners",
                table: "partner",
                column: "document",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_partner_identifier",
                schema: "partners",
                table: "partner",
                column: "identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_phone_contact_id",
                schema: "partners",
                table: "phone",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_phone_identifier",
                schema: "partners",
                table: "phone",
                column: "identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_phone_partner_id",
                schema: "partners",
                table: "phone",
                column: "partner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email",
                schema: "partners");

            migrationBuilder.DropTable(
                name: "phone",
                schema: "partners");

            migrationBuilder.DropTable(
                name: "contact",
                schema: "partners");

            migrationBuilder.DropTable(
                name: "partner",
                schema: "partners");
        }
    }
}
