using DDD.BaseModels;
using DDD.BaseModels.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Test.Infrastructure;
using Xunit;

namespace Test.Cud;

public class BaseCudTests
{
    private static BaseCud<TestDbContext, Sample> CreateCud(TestDb db)
        => new(db.Context, NullLogger<IBaseCud<TestDbContext, Sample>>.Instance);

    [Fact]
    public async Task InsertAsync_Single_PersistsEntity()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);

        var ok = await cud.InsertAsync(new Sample { Name = "a", Value = 1 });

        Assert.True(ok);
        Assert.Equal(1, await db.Context.Samples.CountAsync());
    }

    [Fact]
    public async Task InsertAsync_List_PersistsAll()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);

        var ok = await cud.InsertAsync(new[]
        {
            new Sample { Name = "a", Value = 1 },
            new Sample { Name = "b", Value = 2 },
        });

        Assert.True(ok);
        Assert.Equal(2, await db.Context.Samples.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingEntity()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);
        var entity = new Sample { Name = "a", Value = 1 };
        await cud.InsertAsync(entity);

        entity.Name = "a2";
        entity.Value = 10;
        var ok = await cud.UpdateAsync(entity);

        Assert.True(ok);
        var fromDb = await db.Context.Samples.FindAsync(entity.Id);
        Assert.Equal("a2", fromDb!.Name);
        Assert.Equal(10, fromDb.Value);
    }

    [Fact]
    public async Task DeleteAsync_ById_RemovesEntity()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);
        var entity = new Sample { Name = "a", Value = 1 };
        await cud.InsertAsync(entity);

        var ok = await cud.DeleteAsync(entity.Id);

        Assert.True(ok);
        Assert.Equal(0, await db.Context.Samples.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_RemovesEntity()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);
        var entity = new Sample { Name = "a", Value = 1 };
        await cud.InsertAsync(entity);

        var ok = await cud.DeleteAsync(entity);

        Assert.True(ok);
        Assert.Equal(0, await db.Context.Samples.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_ByPredicate_RemovesMatching()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);
        await cud.InsertAsync(new[] { new Sample { Name = "keep" }, new Sample { Name = "drop" } });

        var ok = await cud.DeleteAsync((Sample s) => s.Name == "drop");

        Assert.True(ok);
        Assert.Equal(1, await db.Context.Samples.CountAsync());
        Assert.Equal("keep", (await db.Context.Samples.SingleAsync()).Name);
    }

    [Fact]
    public async Task InsertAsync_Functional_CreatesWhenNotExists()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);

        var result = await cud.InsertAsync(
            existExpression: s => s.Name == "dup",
            exist: () => "exists",
            create: () => new Sample { Name = "dup", Value = 1 },
            final: created => created is null ? "inserted-null" : "inserted");

        Assert.Equal("inserted", result);
        Assert.Equal(1, await db.Context.Samples.CountAsync());
    }

    [Fact]
    public async Task InsertAsync_Functional_ReturnsExistWhenAlreadyPresent()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);
        await cud.InsertAsync(new Sample { Name = "dup", Value = 1 });

        var result = await cud.InsertAsync(
            existExpression: s => s.Name == "dup",
            exist: () => "exists",
            create: () => new Sample { Name = "dup", Value = 2 },
            final: _ => "inserted");

        Assert.Equal("exists", result);
        Assert.Equal(1, await db.Context.Samples.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_Functional_UpdatesWhenFound()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);
        var entity = new Sample { Name = "u", Value = 1 };
        await cud.InsertAsync(entity);

        var result = await cud.UpdateAsync(
            id: entity.Id,
            notFound: () => "notfound",
            final: updated => updated is null ? "update-failed" : "updated",
            update: e =>
            {
                e.Value = 99;
                return e;
            });

        Assert.Equal("updated", result);
        Assert.Equal(99, (await db.Context.Samples.FindAsync(entity.Id))!.Value);
    }

    [Fact]
    public async Task UpdateAsync_Functional_ReturnsNotFoundForMissingId()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);

        var result = await cud.UpdateAsync(
            id: EntityId.CreateUniqueId(),
            notFound: () => "notfound",
            final: _ => "updated",
            update: e => e);

        Assert.Equal("notfound", result);
    }

    [Fact]
    public async Task Transaction_Commit_PersistsAllChanges()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);

        await cud.BeginTransactionAsync();
        await cud.InsertAsync(new Sample { Name = "a", Value = 1 });
        await cud.InsertAsync(new Sample { Name = "b", Value = 2 });
        var committed = await cud.CommitAsync();

        Assert.True(committed);
        Assert.Equal(2, await db.Context.Samples.CountAsync());
    }

    [Fact]
    public async Task Transaction_Rollback_DiscardsPendingChanges()
    {
        await using var db = DbHelper.CreateDb();
        var cud = CreateCud(db);

        await cud.BeginTransactionAsync();
        await cud.InsertAsync(new Sample { Name = "a", Value = 1 });
        await cud.InsertAsync(new Sample { Name = "b", Value = 2 });
        await cud.RollbackAsync();

        Assert.Equal(0, await db.Context.Samples.CountAsync());
    }
}
