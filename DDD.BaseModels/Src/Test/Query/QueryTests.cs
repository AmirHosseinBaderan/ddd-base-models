using DDD.BaseModels.Service;
using Microsoft.EntityFrameworkCore;
using Test.Infrastructure;
using Xunit;

namespace Test.Query;

public class ActiveSamplesSpec : BaseSpecification<Sample>
{
    public ActiveSamplesSpec(int minValue) : base(s => s.Value >= minValue)
    {
        ApplyOrderByDescending(s => s.Value);
        ApplyPaging(0, 5);
    }
}

public class QueryTests
{
    [Fact]
    public async Task SpecificationEvaluator_AppliesCriteriaOrderAndPaging()
    {
        await using var db = DbHelper.CreateDb();
        db.Context.Samples.AddRange(new[]
        {
            new Sample { Name = "a", Value = 1 },
            new Sample { Name = "b", Value = 5 },
            new Sample { Name = "c", Value = 10 },
        });
        await db.Context.SaveChangesAsync();

        var query = SpecificationEvaluator.GetQuery(db.Context.Samples, new ActiveSamplesSpec(5));
        var result = await query.ToListAsync();

        Assert.Equal(2, result.Count);          // value >= 5
        Assert.Equal(10, result[0].Value);      // ordered descending
        Assert.Equal(5, result[1].Value);
    }

    [Fact]
    public void WhereIf_AppliesOnlyWhenConditionTrue()
    {
        var data = new[]
        {
            new Sample { Name = "a", Value = 1 },
            new Sample { Name = "b", Value = 5 },
        }.AsQueryable();

        var withFilter = data.WhereIf(true, s => s.Value > 1).ToList();
        var withoutFilter = data.WhereIf(false, s => s.Value > 1).ToList();

        Assert.Single(withFilter);
        Assert.Equal(2, withoutFilter.Count);
    }

    [Fact]
    public void OrderByIf_AppliesOnlyWhenConditionTrue()
    {
        var data = new[]
        {
            new Sample { Name = "a", Value = 5 },
            new Sample { Name = "b", Value = 1 },
        }.AsQueryable();

        var ordered = data.OrderByIf(true, s => s.Value, descending: true).ToList();

        Assert.Equal(5, ordered[0].Value);
    }

    [Fact]
    public void WhereContains_FiltersByKeyword()
    {
        var data = new[]
        {
            new Sample { Name = "amir" },
            new Sample { Name = "john" },
        }.AsQueryable();

        var result = data.WhereContains(s => s.Name, "amir").ToList();

        Assert.Single(result);
        Assert.Equal("amir", result[0].Name);
    }

    [Fact]
    public void WhereContains_IgnoresNullKeyword()
    {
        var data = new[]
        {
            new Sample { Name = "amir" },
            new Sample { Name = "john" },
        }.AsQueryable();

        var result = data.WhereContains(s => s.Name, null).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void WhereIn_FiltersByCollection()
    {
        var data = new[]
        {
            new Sample { Name = "a", Value = 1 },
            new Sample { Name = "b", Value = 2 },
            new Sample { Name = "c", Value = 3 },
        }.AsQueryable();

        var result = data.WhereIn(s => s.Value, new[] { 1, 3 }).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ToPagination_Sync_AppliesSkipTake()
    {
        var data = Enumerable.Range(1, 10)
            .Select(i => new Sample { Name = $"s{i}", Value = i })
            .AsQueryable();

        var page = data.ToPagination(1, 3).ToList();

        Assert.Equal(3, page.Count);
        Assert.Equal(4, page[0].Value); // page 1 (zero-based) -> items 4,5,6
    }

    [Fact]
    public async Task ToPaginationAsync_ReturnsItemsAndTotalCount()
    {
        await using var db = DbHelper.CreateDb();
        db.Context.Samples.AddRange(Enumerable.Range(1, 10)
            .Select(i => new Sample { Name = $"s{i}", Value = i }));
        await db.Context.SaveChangesAsync();

        var result = await db.Context.Samples.ToPaginationAsync(page: 1, pageSize: 3);

        Assert.Equal(10, result.ItemsCount);
        Assert.Equal(4, result.PageCount);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public void PaginationExtension_ToPagination_ComputesPageCount()
    {
        var items = new[] { new Sample { Name = "a" } };
        var result = items.ToPagination(itemsCount: 10, pageSize: 3);

        Assert.Equal(10, result.ItemsCount);
        Assert.Equal(4, result.PageCount);
    }

    [Fact]
    public void PaginationExtension_FromPagination_MapsItems()
    {
        var source = new PaginationResult<Sample>(PageCount: 2, ItemsCount: 10,
            Items: new[] { new Sample { Name = "a" } });

        var mapped = source.FromPagination(new[] { "dto-a" });

        Assert.Equal(2, mapped.PageCount);
        Assert.Equal(10, mapped.ItemsCount);
        Assert.Equal("dto-a", mapped.Items.Single());
    }
}
