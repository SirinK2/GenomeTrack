using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GenomeTrack.Application.DTOs.Custody;
using GenomeTrack.Application.Services.Implementation;
using GenomeTrack.Domain.Entity;
using GenomeTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GenomeTrack.UnitTest.Services;

public class CustodyChainTests
{
    private static CustodyService Custody(TestHarness h) =>
        new(h.Db, h.CurrentUser, h.HashChain, h.Clock);

    private static async Task<Sample> SeedSampleAsync(TestHarness h, string location = "Collection Room")
    {
        var sample = new Sample
        {
            Barcode = "BC-0001",
            SubjectRef = "SUBJ-77",
            Type = SampleType.Blood,
            Status = SampleStatus.Registered,
            CollectedAt = h.Clock.UtcNow,
            CurrentLocation = location,
            CreatedAt = h.Clock.UtcNow,
        };

        h.Db.Samples.Add(sample);
        await h.Db.SaveChangesAsync();

        return sample;
    }

    [Fact]
    public async Task First_event_starts_from_the_genesis_hash()
    {
        using var h = new TestHarness();
        var sample = await SeedSampleAsync(h);

        var result = await Custody(h)
            .AppendAsync(sample.Id, new TransferCustodyDto { Action = CustodyAction.Collected, ToLocation = "Bench 1" });

        result.IsSuccess.Should().BeTrue();
        result.Data!.Sequence.Should().Be(1);

        var stored = await h.Db.CustodyEvents.SingleAsync();
        stored.PreviousHash.Should().Be("GENESIS");
        stored.Hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Each_event_links_to_the_hash_of_the_one_before_it()
    {
        using var h = new TestHarness();
        var sample = await SeedSampleAsync(h);
        var custody = Custody(h);

        await custody.AppendAsync(sample.Id, new TransferCustodyDto { Action = CustodyAction.Collected, ToLocation = "Bench 1" });
        h.Clock.Advance(TimeSpan.FromMinutes(10));
        await custody.AppendAsync(sample.Id, new TransferCustodyDto { Action = CustodyAction.Transferred, ToLocation = "Freezer A" });

        var events = await h.Db.CustodyEvents.OrderBy(e => e.Sequence).ToListAsync();

        events.Should().HaveCount(2);
        events[1].PreviousHash.Should().Be(events[0].Hash);
        events[1].Sequence.Should().Be(2);
    }

    [Fact]
    public async Task The_from_location_is_taken_from_the_sample_not_the_caller()
    {
        using var h = new TestHarness();
        var sample = await SeedSampleAsync(h, "Collection Room");

        await Custody(h)
            .AppendAsync(sample.Id, new TransferCustodyDto { Action = CustodyAction.Transferred, ToLocation = "Freezer A" });

        var stored = await h.Db.CustodyEvents.SingleAsync();

        stored.FromLocation.Should().Be("Collection Room");
        stored.ToLocation.Should().Be("Freezer A");

        var sampleAfter = await h.Db.Samples.SingleAsync();
        sampleAfter.CurrentLocation.Should().Be("Freezer A");
    }

    [Fact]
    public async Task An_untouched_chain_verifies()
    {
        using var h = new TestHarness();
        var sample = await SeedSampleAsync(h);
        var custody = Custody(h);

        await custody.AppendAsync(sample.Id, new TransferCustodyDto { Action = CustodyAction.Collected, ToLocation = "Bench 1" });
        h.Clock.Advance(TimeSpan.FromHours(1));
        await custody.AppendAsync(sample.Id, new TransferCustodyDto { Action = CustodyAction.PlacedInStorage, ToLocation = "Freezer A" });

        var report = await custody.VerifyChainAsync(sample.Id);

        report.Data!.IsIntact.Should().BeTrue();
        report.Data.EventCount.Should().Be(2);
        report.Data.BrokenAtSequence.Should().BeNull();
    }

    [Fact]
    public async Task The_context_refuses_to_edit_a_custody_event()
    {
        using var h = new TestHarness();
        var sample = await SeedSampleAsync(h);

        await Custody(h)
            .AppendAsync(sample.Id, new TransferCustodyDto { Action = CustodyAction.Collected, ToLocation = "Bench 1" });

        var stored = await h.Db.CustodyEvents.SingleAsync();
        stored.ToLocation = "Freezer B";

        // The guard is the first line of defence: history cannot be edited through the app at all.
        var act = async () => await h.Db.SaveChangesAsync();

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    [Fact]
    public async Task The_context_refuses_to_delete_a_custody_event()
    {
        using var h = new TestHarness();
        var sample = await SeedSampleAsync(h);

        await Custody(h)
            .AppendAsync(sample.Id, new TransferCustodyDto { Action = CustodyAction.Collected, ToLocation = "Bench 1" });

        h.Db.CustodyEvents.Remove(await h.Db.CustodyEvents.SingleAsync());

        var act = async () => await h.Db.SaveChangesAsync();

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    [Fact]
    public async Task A_row_edited_outside_the_application_is_detected_at_that_link()
    {
        using var h = new TestHarness();
        var sample = await SeedSampleAsync(h);

        // Written straight to the store, the way someone with a database console would. The
        // hashes are computed for "Freezer A" but the row says "Freezer B", which is exactly the
        // state left behind by an UPDATE that did not rewrite the chain.
        var first = RawEvent(h, sample.Id, 1, "GENESIS", CustodyAction.Collected, "Collection Room", "Bench 1");
        var second = RawEvent(h, sample.Id, 2, first.Hash, CustodyAction.Transferred, "Bench 1", "Freezer A");
        second.ToLocation = "Freezer B";
        var third = RawEvent(h, sample.Id, 3, second.Hash, CustodyAction.RemovedFromStorage, "Freezer A", "Bench 2");

        h.Db.CustodyEvents.AddRange(first, second, third);
        await h.Db.SaveChangesAsync();

        var report = await Custody(h).VerifyChainAsync(sample.Id);

        report.Data!.IsIntact.Should().BeFalse();
        report.Data.BrokenAtSequence.Should().Be(2);
        report.Data.Explanation.Should().Contain("edited");
    }

    [Fact]
    public async Task A_row_deleted_outside_the_application_is_detected_as_a_sequence_gap()
    {
        using var h = new TestHarness();
        var sample = await SeedSampleAsync(h);

        var first = RawEvent(h, sample.Id, 1, "GENESIS", CustodyAction.Collected, "Collection Room", "Bench 1");
        var second = RawEvent(h, sample.Id, 2, first.Hash, CustodyAction.Transferred, "Bench 1", "Freezer A");
        var third = RawEvent(h, sample.Id, 3, second.Hash, CustodyAction.RemovedFromStorage, "Freezer A", "Bench 2");

        // Event 2 never makes it in — the gap a DELETE leaves behind.
        h.Db.CustodyEvents.AddRange(first, third);
        await h.Db.SaveChangesAsync();

        var report = await Custody(h).VerifyChainAsync(sample.Id);

        report.Data!.IsIntact.Should().BeFalse();
        report.Data.BrokenAtSequence.Should().Be(3);
        report.Data.Explanation.Should().Contain("removed");
    }

    [Fact]
    public async Task A_relinked_chain_is_detected_even_when_every_row_hashes_correctly()
    {
        using var h = new TestHarness();
        var sample = await SeedSampleAsync(h);

        var first = RawEvent(h, sample.Id, 1, "GENESIS", CustodyAction.Collected, "Collection Room", "Bench 1");

        // Each row's own hash is internally consistent, but the second points at the genesis
        // constant rather than at the first — a spliced chain that a per-row check would miss.
        var second = RawEvent(h, sample.Id, 2, "GENESIS", CustodyAction.Transferred, "Bench 1", "Freezer A");

        h.Db.CustodyEvents.AddRange(first, second);
        await h.Db.SaveChangesAsync();

        var report = await Custody(h).VerifyChainAsync(sample.Id);

        report.Data!.IsIntact.Should().BeFalse();
        report.Data.BrokenAtSequence.Should().Be(2);
        report.Data.Explanation.Should().Contain("re-linked");
    }

    /// <summary>
    /// Builds a correctly hashed event without going through the service, so a test can then
    /// break exactly one property of it.
    /// </summary>
    private static CustodyEvent RawEvent(
        TestHarness h,
        Guid sampleId,
        int sequence,
        string previousHash,
        CustodyAction action,
        string from,
        string to
    )
    {
        var occurredAt = h.Clock.UtcNow.AddMinutes(sequence);

        return new CustodyEvent
        {
            SampleId = sampleId,
            Sequence = sequence,
            PreviousHash = previousHash,
            Action = action,
            FromLocation = from,
            ToLocation = to,
            ActorId = h.CurrentUser.UserId,
            OccurredAt = occurredAt,
            CreatedAt = occurredAt,
            Hash = h.HashChain.Compute(
                previousHash,
                sampleId,
                sequence,
                action,
                from,
                to,
                h.CurrentUser.UserId,
                occurredAt
            ),
        };
    }

    [Fact]
    public async Task Sub_millisecond_precision_is_dropped_before_the_hash_is_taken()
    {
        using var h = new TestHarness();
        var sample = await SeedSampleAsync(h);

        // A wall clock hands out 100-nanosecond ticks. Postgres keeps microseconds and other
        // providers keep less, so anything finer than a millisecond has to be gone before the
        // hash is taken — otherwise the database rounds the stored value and the row stops
        // reproducing its own hash. This is what made every live chain verify as broken.
        h.Clock.SetTicksWithin(7_777);

        await Custody(h)
            .AppendAsync(sample.Id, new TransferCustodyDto { Action = CustodyAction.Collected, ToLocation = "Bench 1" });

        var stored = await h.Db.CustodyEvents.SingleAsync();

        (stored.OccurredAt.Ticks % TimeSpan.TicksPerMillisecond).Should().Be(0);

        var report = await Custody(h).VerifyChainAsync(sample.Id);
        report.Data!.IsIntact.Should().BeTrue();
    }

    [Fact]
    public async Task Appending_to_a_missing_sample_is_not_found()
    {
        using var h = new TestHarness();

        var result = await Custody(h)
            .AppendAsync(Guid.NewGuid(), new TransferCustodyDto { Action = CustodyAction.Collected, ToLocation = "Bench 1" });

        result.IsSuccess.Should().BeFalse();
        result.IsNotFound.Should().BeTrue();
    }
}
