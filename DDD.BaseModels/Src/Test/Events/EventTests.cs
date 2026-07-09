using DDD.BaseModels;
using Microsoft.Extensions.DependencyInjection;
using Test.Infrastructure;
using Xunit;

namespace Test.Events;

public class DomainEventTests
{
    [Fact]
    public async Task DispatchAsync_InvokesRegisteredHandler()
    {
        SampleEventHandler.Reset();
        await using var provider = DbHelper.CreateDispatcherProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();

        await dispatcher.DispatchAsync(new List<IDomainEvent> { new SampleEvent() }, CancellationToken.None);

        Assert.Equal(1, SampleEventHandler.CallCount);
        Assert.NotNull(SampleEventHandler.LastEvent);
    }

    [Fact]
    public async Task BeforeSaveChanges_DispatchesAndClearsAggregateEvents()
    {
        SampleEventHandler.Reset();
        await using var provider = DbHelper.CreateDispatcherProvider();
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();

        var db = DbHelper.CreateDb();
        try
        {
            var sample = new Sample { Name = "before-save" };
            sample.Raise(new SampleEvent { SampleId = sample.Id });
            db.Context.Samples.Add(sample);

            await db.Context.BeforeSaveChanges(dispatcher, CancellationToken.None);

            Assert.Equal(1, SampleEventHandler.CallCount);
            Assert.Empty(sample.Events); // events cleared after dispatch
        }
        finally
        {
            await db.DisposeAsync();
        }
    }
}
