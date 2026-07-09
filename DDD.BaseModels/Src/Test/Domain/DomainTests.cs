using DDD.BaseModels;
using Test.Infrastructure;
using Xunit;

namespace Test.DomainModels;

public class EntityIdTests
{
    [Fact]
    public void CreateUniqueId_GeneratesNewGuid()
    {
        var a = EntityId.CreateUniqueId();
        var b = EntityId.CreateUniqueId();

        Assert.NotEqual(a.Value, b.Value);
        Assert.NotEqual(Guid.Empty, a.Value);
    }

    [Fact]
    public void ImplicitCast_FromGuid_Works()
    {
        var guid = Guid.NewGuid();
        EntityId id = guid;

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void ExplicitCast_ToGuid_Works()
    {
        var id = EntityId.CreateUniqueId();
        Guid guid = (Guid)id;

        Assert.Equal(id.Value, guid);
    }

    [Fact]
    public void Equality_BetweenEntityIdAndGuid_Works()
    {
        var guid = Guid.NewGuid();
        var id = EntityId.Create(guid);

        Assert.True(id == guid);
        Assert.True(guid == id);
        Assert.False(id != guid);
    }

    [Fact]
    public void Equality_BetweenTwoEntityIds_Works()
    {
        var guid = Guid.NewGuid();
        var a = EntityId.Create(guid);
        var b = EntityId.Create(guid);
        var c = EntityId.CreateUniqueId();

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.NotEqual(a, c);
        Assert.False(a == c);
    }

    [Fact]
    public void Create_NullableGuid_ReturnsNullForNull()
    {
        Assert.Null(EntityId.Create((Guid?)null));
        Assert.NotNull(EntityId.Create((Guid?)Guid.NewGuid()));
    }
}

public class Money : ValueObject<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

public class ValueObjectTests
{
    [Fact]
    public void Equal_WhenComponentsMatch()
    {
        var a = new Money(10, "USD");
        var b = new Money(10, "USD");

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void NotEqual_WhenComponentsDiffer()
    {
        var a = new Money(10, "USD");
        var b = new Money(10, "EUR");

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void HashCode_MatchesForEqualInstances()
    {
        var a = new Money(5, "IRR");
        var b = new Money(5, "IRR");

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}

public class AggregateRootTests
{
    private class TestAggregate : AggregateRootBase
    {
        public string Name { get; set; } = null!;

        public void Raise(IDomainEvent @event) => AddEvent(@event);
    }

    [Fact]
    public void AddEvent_CollectsEvents()
    {
        var agg = new TestAggregate();
        agg.Raise(new SampleEvent());

        Assert.Single(agg.Events);
    }

    [Fact]
    public void ClearEvents_RemovesAllEvents()
    {
        var agg = new TestAggregate();
        agg.Raise(new SampleEvent());
        agg.Raise(new SampleEvent());

        Assert.Equal(2, agg.Events.Count);
        agg.ClearEvents();

        Assert.Empty(agg.Events);
    }
}
