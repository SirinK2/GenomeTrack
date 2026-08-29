using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GenomeTrack.Application.DTOs.Sample;
using GenomeTrack.Application.Services.Implementation;
using GenomeTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GenomeTrack.UnitTest.Services;

public class SampleServiceTests
{
    private static SampleService Service(TestHarness h) =>
        new(h.Db, new CustodyService(h.Db, h.CurrentUser, h.HashChain, h.Clock), h.Clock);

    private static RegisterSampleDto Registration(string barcode = "BC-1000") =>
        new()
        {
            Barcode = barcode,
            SubjectRef = "SUBJ-1",
            Type = SampleType.Blood,
            CollectedAt = new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.Zero),
            CollectedAtLocation = "Collection Room",
        };

    [Fact]
    public async Task Registering_opens_the_chain_at_collection()
    {
        using var h = new TestHarness();

        var result = await Service(h).RegisterAsync(Registration());

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(SampleStatus.Registered);

        // The first link must exist before the lab ever touches the tube, so a sample lost in
        // transit still has a documented origin.
        var events = await h.Db.CustodyEvents.ToListAsync();
        events.Should().ContainSingle();
        events[0].Action.Should().Be(CustodyAction.Collected);
    }

    [Fact]
    public async Task A_live_barcode_cannot_be_reused()
    {
        using var h = new TestHarness();
        var service = Service(h);

        await service.RegisterAsync(Registration("BC-DUP"));
        var second = await service.RegisterAsync(Registration("BC-DUP"));

        second.IsSuccess.Should().BeFalse();
        second.IsConflict.Should().BeTrue();
    }

    [Fact]
    public async Task A_barcode_frees_up_once_its_sample_is_gone()
    {
        using var h = new TestHarness();
        var service = Service(h);

        var first = await service.RegisterAsync(Registration("BC-RECYCLE"));

        var sample = await h.Db.Samples.SingleAsync(s => s.Id == first.Data!.Id);
        sample.IsDeleted = true;
        await h.Db.SaveChangesAsync();

        var second = await service.RegisterAsync(Registration("BC-RECYCLE"));

        second.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Accessioning_moves_the_sample_and_records_receipt()
    {
        using var h = new TestHarness();
        var service = Service(h);

        var registered = await service.RegisterAsync(Registration());
        h.Clock.Advance(TimeSpan.FromHours(2));

        var accessioned = await service.AccessionAsync(
            registered.Data!.Id,
            new AccessionSampleDto { ReceivedAtLocation = "Accessioning Bench", Note = "Seal intact." }
        );

        accessioned.IsSuccess.Should().BeTrue();
        accessioned.Data!.Status.Should().Be(SampleStatus.Accessioned);
        accessioned.Data.CurrentLocation.Should().Be("Accessioning Bench");

        var events = await h.Db.CustodyEvents.OrderBy(e => e.Sequence).ToListAsync();
        events.Should().HaveCount(2);
        events[1].Action.Should().Be(CustodyAction.Received);
        events[1].FromLocation.Should().Be("Collection Room");
    }

    [Fact]
    public async Task A_sample_cannot_be_accessioned_twice()
    {
        using var h = new TestHarness();
        var service = Service(h);

        var registered = await service.RegisterAsync(Registration());
        await service.AccessionAsync(registered.Data!.Id, new AccessionSampleDto { ReceivedAtLocation = "Bench" });

        var again = await service.AccessionAsync(
            registered.Data.Id,
            new AccessionSampleDto { ReceivedAtLocation = "Bench" }
        );

        again.IsSuccess.Should().BeFalse();
        again.IsConflict.Should().BeTrue();
    }

    [Fact]
    public async Task An_oversized_page_is_clamped_rather_than_reset()
    {
        using var h = new TestHarness();
        var service = Service(h);

        for (var i = 0; i < 5; i++)
            await service.RegisterAsync(Registration($"BC-{i:D4}"));

        var page = await service.SearchAsync(new SampleFilter { PageSize = 5000 });

        // Clamped to the ceiling, not silently reset to the default — a caller asking for
        // everything gets as much as the API allows.
        page.Data!.Pagination.PageSize.Should().Be(100);
        page.Data.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task Registering_without_a_barcode_fails()
    {
        using var h = new TestHarness();

        var result = await Service(h).RegisterAsync(Registration("   "));

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Barcode");
    }
}
