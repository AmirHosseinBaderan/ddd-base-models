using Microsoft.EntityFrameworkCore;

namespace DDD.BaseModels;

public static class PipelineExtensions
{
    public static async Task BeforeSaveChanges(this DbContext dbContext, IDomainEventDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var entitiesWithEvents = dbContext.ChangeTracker
            .Entries<AggregateRootBase>()
            .Where(e => e.Entity.Events.Any())
            .Select(e => e.Entity)
            .ToList();

        var allEvents = entitiesWithEvents.SelectMany(e => e.Events).ToList();

        foreach (var entity in entitiesWithEvents)
            entity.ClearEvents();

        if (allEvents.Any())
            await dispatcher.DispatchAsync(allEvents, cancellationToken);
    }
}