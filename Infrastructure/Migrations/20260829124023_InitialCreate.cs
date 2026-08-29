using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenomeTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lab_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "samples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubjectRef = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CollectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrentLocation = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_samples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "variants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Gene = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Chromosome = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Position = table.Column<long>(type: "bigint", nullable: false),
                    ReferenceAllele = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AlternateAllele = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Significance = table.Column<int>(type: "integer", nullable: false),
                    ClinVarId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sequencing_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Platform = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sequencing_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sequencing_runs_lab_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "lab_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "custody_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    FromLocation = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ToLocation = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custody_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_custody_events_lab_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "lab_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_custody_events_samples_SampleId",
                        column: x => x.SampleId,
                        principalTable: "samples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "run_samples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SequencingRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    LaneIndex = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_run_samples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_run_samples_samples_SampleId",
                        column: x => x.SampleId,
                        principalTable: "samples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_run_samples_sequencing_runs_SequencingRunId",
                        column: x => x.SequencingRunId,
                        principalTable: "sequencing_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variant_calls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequencingRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadDepth = table.Column<int>(type: "integer", nullable: false),
                    QualityScore = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Zygosity = table.Column<int>(type: "integer", nullable: false),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReleasedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variant_calls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_variant_calls_lab_users_ReleasedById",
                        column: x => x.ReleasedById,
                        principalTable: "lab_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_variant_calls_samples_SampleId",
                        column: x => x.SampleId,
                        principalTable: "samples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_variant_calls_sequencing_runs_SequencingRunId",
                        column: x => x.SequencingRunId,
                        principalTable: "sequencing_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_variant_calls_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_custody_events_ActorId",
                table: "custody_events",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_custody_events_SampleId_Sequence",
                table: "custody_events",
                columns: new[] { "SampleId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lab_users_Email",
                table: "lab_users",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_run_samples_SampleId",
                table: "run_samples",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_run_samples_SequencingRunId_LaneIndex",
                table: "run_samples",
                columns: new[] { "SequencingRunId", "LaneIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_run_samples_SequencingRunId_SampleId",
                table: "run_samples",
                columns: new[] { "SequencingRunId", "SampleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_samples_Barcode",
                table: "samples",
                column: "Barcode",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_samples_Status",
                table: "samples",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_samples_SubjectRef",
                table: "samples",
                column: "SubjectRef");

            migrationBuilder.CreateIndex(
                name: "IX_sequencing_runs_CreatedById",
                table: "sequencing_runs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_sequencing_runs_RunCode",
                table: "sequencing_runs",
                column: "RunCode",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_variant_calls_ReleasedById",
                table: "variant_calls",
                column: "ReleasedById");

            migrationBuilder.CreateIndex(
                name: "IX_variant_calls_SampleId_VariantId_SequencingRunId",
                table: "variant_calls",
                columns: new[] { "SampleId", "VariantId", "SequencingRunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variant_calls_SequencingRunId",
                table: "variant_calls",
                column: "SequencingRunId");

            migrationBuilder.CreateIndex(
                name: "IX_variant_calls_VariantId",
                table: "variant_calls",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_variants_Chromosome_Position_ReferenceAllele_AlternateAllele",
                table: "variants",
                columns: new[] { "Chromosome", "Position", "ReferenceAllele", "AlternateAllele" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variants_Gene",
                table: "variants",
                column: "Gene");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custody_events");

            migrationBuilder.DropTable(
                name: "run_samples");

            migrationBuilder.DropTable(
                name: "variant_calls");

            migrationBuilder.DropTable(
                name: "samples");

            migrationBuilder.DropTable(
                name: "sequencing_runs");

            migrationBuilder.DropTable(
                name: "variants");

            migrationBuilder.DropTable(
                name: "lab_users");
        }
    }
}
