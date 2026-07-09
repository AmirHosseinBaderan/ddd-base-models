using DDD.BaseModels;
using DDD.BaseModels.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Test.Infrastructure;
using Xunit;

namespace Test.Cud;

/// <summary>Holds a FluentCud backed by an in-memory SQLite database and a dispatcher with the test handler.</summary>
public sealed class FluentFixture : IAsyncDisposable
{
    internal FluentCud<TestDbContext, Sample> Fluent { get; }
    public TestDbContext Context { get; }
    private readonly TestDb _db;
    private readonly ServiceProvider _provider;

    public FluentFixture()
    {
        _db = DbHelper.CreateDb();
        Context = _db.Context;
        var cud = new BaseCud<TestDbContext, Sample>(Context,
            NullLogger<IBaseCud<TestDbContext, Sample>>.Instance);
        _provider = DbHelper.CreateDispatcherProvider();
        var dispatcher = _provider.GetRequiredService<IDomainEventDispatcher>();
        Fluent = new FluentCud<TestDbContext, Sample>(cud, dispatcher);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _provider.DisposeAsync();
    }
}

public class FluentCudTests
{
    [Fact]
    public async Task AddAsync_DispatchEvent_ConditionTrue_DispatchesTrueEvent()
    {
        SampleEventHandler.Reset();
        await using var fixture = new FluentFixture();

        var ok = await fixture.Fluent.AddAsync(new Sample { Name = "a", Value = 1 })
            .DispatchEvent(true, () => new SampleEvent(), () => new SampleEvent());

        Assert.True(ok);
        Assert.Equal(1, SampleEventHandler.CallCount);
        Assert.Equal(1, await fixture.Context.Samples.CountAsync());
    }

    [Fact]
    public async Task AddAsync_DispatchEvent_ConditionFalse_DispatchesFalseEvent()
    {
        SampleEventHandler.Reset();
        await using var fixture = new FluentFixture();

        var ok = await fixture.Fluent.AddAsync(new Sample { Name = "a", Value = 1 })
            .DispatchEvent(false, () => new SampleEvent(), () => new SampleEvent());

        Assert.True(ok);
        Assert.Equal(1, SampleEventHandler.CallCount);
    }

    [Fact]
    public async Task AddAsync_DispatchEventIf_OnlyWhenConditionTrue()
    {
        SampleEventHandler.Reset();
        await using var fixture = new FluentFixture();

        await fixture.Fluent.AddAsync(new Sample { Name = "a" }).DispatchEventIf(false, () => new SampleEvent());
        Assert.Equal(0, SampleEventHandler.CallCount);

        await fixture.Fluent.AddAsync(new Sample { Name = "b" }).DispatchEventIf(true, () => new SampleEvent());
        Assert.Equal(1, SampleEventHandler.CallCount);
    }

    [Fact]
    public async Task AddAsync_DispatchEvent_OnSuccess()
    {
        SampleEventHandler.Reset();
        await using var fixture = new FluentFixture();

        await fixture.Fluent.AddAsync(new Sample { Name = "a" }).DispatchEvent(() => new SampleEvent());

        Assert.Equal(1, SampleEventHandler.CallCount);
    }

    [Fact]
    public async Task AddAsync_DispatchEvent_OnResult_ReceivesSuccess()
    {
        SampleEventHandler.Reset();
        await using var fixture = new FluentFixture();

        await fixture.Fluent.AddAsync(new Sample { Name = "a" })
            .DispatchEvent(success => new SampleEvent { SampleId = success ? EntityId.CreateUniqueId() : null });

        Assert.Equal(1, SampleEventHandler.CallCount);
        Assert.NotNull(SampleEventHandler.LastEvent);
    }

    [Fact]
    public async Task Transactional_BeginAddCommit_PersistsAndDispatches()
    {
        SampleEventHandler.Reset();
        await using var fixture = new FluentFixture();

        await fixture.Fluent.BeginTransactionAsync();
        await fixture.Fluent.AddAsync(new Sample { Name = "a", Value = 1 })
            .DispatchEvent(() => new SampleEvent());
        var committed = await fixture.Fluent.CommitAsync();

        Assert.True(committed);
        Assert.Equal(1, await fixture.Context.Samples.CountAsync());
        Assert.Equal(1, SampleEventHandler.CallCount);
    }

    [Fact]
    public async Task Transactional_Rollback_DiscardsChanges()
    {
        SampleEventHandler.Reset();
        await using var fixture = new FluentFixture();

        await fixture.Fluent.BeginTransactionAsync();
        await fixture.Fluent.AddAsync(new Sample { Name = "a" }).DispatchEvent(() => new SampleEvent());
        await fixture.Fluent.RollbackAsync();

        Assert.Equal(0, await fixture.Context.Samples.CountAsync());
    }
}
