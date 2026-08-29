using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GenomeTrack.Application.DTOs.Run;
using GenomeTrack.Application.DTOs.Sample;
using GenomeTrack.Application.DTOs.Variant;
using GenomeTrack.Application.Services.Implementation;
using GenomeTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GenomeTrack.UnitTest.Services;

public class RunAndReleaseTests
{
    private static SampleService Samples(TestHarness h) =>
        new(h.Db, new CustodyService(h.Db, h.CurrentUser, h.HashChain, h.Clock), h.Clock);

    private static SequencingRunService Runs(TestHarness h) => new(h.Db, h.CurrentUser, h.Clock);

    private static VariantService Variants(TestHarness h) => new(h.Db, h.CurrentUser, h.Clock);

    private static async Task<Guid> AccessionedSampleAsync(TestHarness h, string barcode)
    {
        var service = Samples(h);

        var registered = await service.RegisterAsync(
            new RegisterSampleDto
            {
                Barcode = barcode,
                SubjectRef = "SUBJ-1",
                Type = SampleType.Blood,
                CollectedAt = h.Clock.UtcNow,
                CollectedAtLocation = "Collection Room",
            }
        );

        await service.AccessionAsync(
            registered.Data!.Id,
            new AccessionSampleDto { ReceivedAtLocation = "Accessioning Bench" }
        );

        return registered.Data.Id;
    }

    private static VariantCallInputDto Call(Guid sampleId, ClinicalSignificance significance = ClinicalSignificance.Pathogenic) =>
        new()
        {
            SampleId = sampleId,
            Gene = "BRCA1",
            Chromosome = "17",
            Position = 43_094_464,
            ReferenceAllele = "A",
            AlternateAllele = "G",
            Significance = significance,
            ReadDepth = 120,
            QualityScore = 99.5m,
            Zygosity = Zygosity.Heterozygous,
        };

    [Fact]
    public async Task A_sample_that_was_never_accessioned_cannot_be_loaded()
    {
        using var h = new TestHarness();

        var registered = await Samples(h)
            .RegisterAsync(
                new RegisterSampleDto
                {
                    Barcode = "BC-NOACC",
                    SubjectRef = "SUBJ-9",
                    Type = SampleType.Saliva,
                    CollectedAt = h.Clock.UtcNow,
                    CollectedAtLocation = "Clinic",
                }
            );

        var run = await Runs(h).CreateAsync(new CreateRunDto { RunCode = "RUN-1", Platform = "NovaSeq" });

        var load = await Runs(h)
            .LoadSampleAsync(run.Data!.Id, new LoadSampleDto { SampleId = registered.Data!.Id, LaneIndex = 1 });

        // This is the rule the accessioning step exists to enforce: material the lab never
        // confirmed receiving must never reach a flow cell.
        load.IsSuccess.Should().BeFalse();
        load.IsConflict.Should().BeTrue();
        load.Message.Should().Contain("accessioned");
    }

    [Fact]
    public async Task Two_samples_cannot_share_a_lane()
    {
        using var h = new TestHarness();
        var first = await AccessionedSampleAsync(h, "BC-L1");
        var second = await AccessionedSampleAsync(h, "BC-L2");

        var run = await Runs(h).CreateAsync(new CreateRunDto { RunCode = "RUN-2", Platform = "NovaSeq" });

        await Runs(h).LoadSampleAsync(run.Data!.Id, new LoadSampleDto { SampleId = first, LaneIndex = 1 });
        var clash = await Runs(h).LoadSampleAsync(run.Data.Id, new LoadSampleDto { SampleId = second, LaneIndex = 1 });

        clash.IsSuccess.Should().BeFalse();
        clash.Message.Should().Contain("Lane 1");
    }

    [Fact]
    public async Task An_empty_run_cannot_start()
    {
        using var h = new TestHarness();
        var run = await Runs(h).CreateAsync(new CreateRunDto { RunCode = "RUN-3", Platform = "NovaSeq" });

        var start = await Runs(h).StartAsync(run.Data!.Id);

        start.IsSuccess.Should().BeFalse();
        start.Message.Should().Contain("at least one sample");
    }

    [Fact]
    public async Task Starting_a_run_puts_its_samples_into_sequencing()
    {
        using var h = new TestHarness();
        var sampleId = await AccessionedSampleAsync(h, "BC-SEQ");
        var run = await Runs(h).CreateAsync(new CreateRunDto { RunCode = "RUN-4", Platform = "NovaSeq" });
        await Runs(h).LoadSampleAsync(run.Data!.Id, new LoadSampleDto { SampleId = sampleId, LaneIndex = 1 });

        var started = await Runs(h).StartAsync(run.Data.Id);

        started.Data!.Status.Should().Be(RunStatus.Running);
        (await h.Db.Samples.SingleAsync(s => s.Id == sampleId)).Status.Should().Be(SampleStatus.InSequencing);
    }

    [Fact]
    public async Task A_call_for_a_sample_not_on_the_run_is_rejected_with_the_offending_id()
    {
        using var h = new TestHarness();
        var onRun = await AccessionedSampleAsync(h, "BC-ON");
        var offRun = await AccessionedSampleAsync(h, "BC-OFF");

        var run = await Runs(h).CreateAsync(new CreateRunDto { RunCode = "RUN-5", Platform = "NovaSeq" });
        await Runs(h).LoadSampleAsync(run.Data!.Id, new LoadSampleDto { SampleId = onRun, LaneIndex = 1 });
        await Runs(h).StartAsync(run.Data.Id);

        var complete = await Runs(h)
            .CompleteAsync(run.Data.Id, new CompleteRunDto { Calls = new List<VariantCallInputDto> { Call(offRun) } });

        complete.IsSuccess.Should().BeFalse();
        complete.Details.Should().ContainSingle();
        complete.Details![0].Message.Should().Contain(offRun.ToString());

        // Nothing was written: the payload is validated as a whole before any of it lands.
        (await h.Db.VariantCalls.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task The_same_substitution_in_two_samples_resolves_to_one_variant()
    {
        using var h = new TestHarness();
        var first = await AccessionedSampleAsync(h, "BC-V1");
        var second = await AccessionedSampleAsync(h, "BC-V2");

        var run = await Runs(h).CreateAsync(new CreateRunDto { RunCode = "RUN-6", Platform = "NovaSeq" });
        await Runs(h).LoadSampleAsync(run.Data!.Id, new LoadSampleDto { SampleId = first, LaneIndex = 1 });
        await Runs(h).LoadSampleAsync(run.Data.Id, new LoadSampleDto { SampleId = second, LaneIndex = 2 });
        await Runs(h).StartAsync(run.Data.Id);

        await Runs(h)
            .CompleteAsync(
                run.Data.Id,
                new CompleteRunDto { Calls = new List<VariantCallInputDto> { Call(first), Call(second) } }
            );

        // Two calls, one catalogued variant — otherwise "how many subjects carry this?" is
        // uncountable.
        (await h.Db.VariantCalls.CountAsync()).Should().Be(2);
        (await h.Db.Variants.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Only_a_principal_investigator_may_release()
    {
        using var h = new TestHarness();
        var callId = await CompletedCallAsync(h);

        h.CurrentUser.Role = LabRole.Analyst;
        var asAnalyst = await Variants(h).ReleaseAsync(callId);

        asAnalyst.IsSuccess.Should().BeFalse();
        asAnalyst.IsForbidden.Should().BeTrue();

        h.CurrentUser.Role = LabRole.PrincipalInvestigator;
        var asPi = await Variants(h).ReleaseAsync(callId);

        asPi.IsSuccess.Should().BeTrue();
        asPi.Data!.IsReleased.Should().BeTrue();
    }

    [Fact]
    public async Task A_call_cannot_be_released_twice()
    {
        using var h = new TestHarness();
        var callId = await CompletedCallAsync(h);
        h.CurrentUser.Role = LabRole.PrincipalInvestigator;

        await Variants(h).ReleaseAsync(callId);
        var again = await Variants(h).ReleaseAsync(callId);

        again.IsSuccess.Should().BeFalse();
        again.IsConflict.Should().BeTrue();
    }

    [Fact]
    public async Task A_technician_never_sees_an_unreleased_call()
    {
        using var h = new TestHarness();
        await CompletedCallAsync(h);

        h.CurrentUser.Role = LabRole.Technician;
        var visible = await Variants(h).SearchAsync(new VariantCallFilter());

        // Unreleased calls are provisional interpretations and stay inside the analyst boundary
        // regardless of what the caller asked for.
        visible.Data!.Items.Should().BeEmpty();

        h.CurrentUser.Role = LabRole.Analyst;
        var toAnalyst = await Variants(h).SearchAsync(new VariantCallFilter());
        toAnalyst.Data!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Filtering_by_minimum_significance_excludes_benign_findings()
    {
        using var h = new TestHarness();
        var sampleId = await AccessionedSampleAsync(h, "BC-SIG");
        var run = await Runs(h).CreateAsync(new CreateRunDto { RunCode = "RUN-SIG", Platform = "NovaSeq" });
        await Runs(h).LoadSampleAsync(run.Data!.Id, new LoadSampleDto { SampleId = sampleId, LaneIndex = 1 });
        await Runs(h).StartAsync(run.Data.Id);

        var benign = Call(sampleId, ClinicalSignificance.Benign);
        benign.Position = 43_094_500;

        await Runs(h)
            .CompleteAsync(
                run.Data.Id,
                new CompleteRunDto { Calls = new List<VariantCallInputDto> { Call(sampleId), benign } }
            );

        h.CurrentUser.Role = LabRole.Analyst;

        var actionable = await Variants(h)
            .SearchAsync(new VariantCallFilter { MinimumSignificance = ClinicalSignificance.LikelyPathogenic });

        actionable.Data!.Items.Should().ContainSingle();
        actionable.Data.Items[0].Significance.Should().Be(ClinicalSignificance.Pathogenic);
    }

    private static async Task<Guid> CompletedCallAsync(TestHarness h)
    {
        var sampleId = await AccessionedSampleAsync(h, $"BC-{Guid.NewGuid():N}".Substring(0, 12));
        var run = await Runs(h).CreateAsync(new CreateRunDto { RunCode = $"RUN-{Guid.NewGuid():N}".Substring(0, 12), Platform = "NovaSeq" });
        await Runs(h).LoadSampleAsync(run.Data!.Id, new LoadSampleDto { SampleId = sampleId, LaneIndex = 1 });
        await Runs(h).StartAsync(run.Data.Id);
        await Runs(h).CompleteAsync(run.Data.Id, new CompleteRunDto { Calls = new List<VariantCallInputDto> { Call(sampleId) } });

        return (await h.Db.VariantCalls.FirstAsync()).Id;
    }
}
