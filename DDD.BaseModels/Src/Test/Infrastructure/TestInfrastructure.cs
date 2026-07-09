using DDD.BaseModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace Test.Infrastructure;

/// <summary>A simple aggregate used across the CUD / event tests.</summary>
public class Sample : AggregateRootBase
{
    public Sample()
    {
        Id = EntityId.CreateUniqueId();
    }

    public string Name { get; set; } = null!;
    public int Value { get; set; }

    /// <summary>Test-only helper to expose the protected <see cref="AggregateRootBase.AddEvent{T}"/>.</summary>
    public void Raise(IDomainEvent @event) => AddEvent(@event);
}

/// <summary>A simple domain event used by the dispatcher / pipeline tests.</summary>
public record SampleEvent : IDomainEvent
{
    public DateTime CreatedOn { get; set; }
    public EntityId? SampleId { get; init; }
}

/// <summary>Test handler that records how many times it was invoked.</summary>
public class SampleEventHandler : IDomainEventHandler<SampleEvent>
{
    public static int CallCount;
    public static SampleEvent? LastEvent;

    public Task HandleAsync(SampleEvent domainEvent, CancellationToken ct = default)
    {
        CallCount++;
        LastEvent = domainEvent;
        return Task.CompletedTask;
    }

    public static void Reset()
    {
        CallCount = 0;
        LastEvent = null;
    }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<Sample> Samples => Set<Sample>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entityIdConverter = new ValueConverter<EntityId, Guid>(
            id => id.Value,
            value => EntityId.Create(value));

        modelBuilder.Entity<Sample>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id)
                .HasConversion(entityIdConverter)
                .ValueGeneratedNever();
            builder.Ignore(s => s.Events);
        });
    }
}

/// <summary>Disposable wrapper around an in-memory SQLite database and its connection.</summary>
public sealed class TestDb : IAsyncDisposable
{
    public TestDbContext Context { get; }
    public SqliteConnection Connection { get; }

    public TestDb(TestDbContext context, SqliteConnection connection)
    {
        Context = context;
        Connection = connection;
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}

/// <summary>Helpers for spinning up an in-memory SQLite database (supports transactions).</summary>
public static class DbHelper
{
    public static TestDb CreateDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return new TestDb(context, connection);
    }

    /// <summary>Builds a service provider that can resolve IDomainEventDispatcher with the test handler.</summary>
    public static ServiceProvider CreateDispatcherProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler<SampleEvent>, SampleEventHandler>();
        return services.BuildServiceProvider();
    }
}
