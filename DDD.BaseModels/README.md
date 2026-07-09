# DDD.BaseModels

A small, opinionated Domain-Driven Design toolkit for .NET 8 built on top of Entity Framework Core.
It provides base model types, a domain-event dispatcher, a classic and a **fluent** CUD (Create/Update/Delete)
service, the Specification pattern, pagination helpers, and a dynamic DTO builder.

> Package: `DDD.BaseModels` (v1.7.0) · Target framework: `net8.0` · Depends on `Microsoft.EntityFrameworkCore` 8.

---

## Table of Contents
1. [Getting Started (Dependency Injection)](#1-getting-started-dependency-injection)
2. [Domain Model](#2-domain-model)
3. [Domain Events](#3-domain-events)
4. [Classic CUD — `IBaseCud` / `BaseCud`](#4-classic-cud--ibasecud--basecud)
5. [Fluent CUD — `IFluentCud` / `FluentCud`](#5-fluent-cud--ifluentcud--fluentcud)
6. [Transactional Operations](#6-transactional-operations)
7. [Querying — Specifications & Extensions](#7-querying--specifications--extensions)
8. [Pagination](#8-pagination)
9. [Dynamic DTO Builder](#9-dynamic-dto-builder)
10. [Class Reference](#10-class-reference)

---

## 1. Getting Started (Dependency Injection)

Register the services and domain-event infrastructure once at startup:

```csharp
using DDD.BaseModels;

// In Program.cs / Startup.cs
builder.Services.AddDDDBaseServices();          // registers IBaseCud<,> and IFluentCud<,>
builder.Services.ConfigDDDEvents(typeof(Program).Assembly); // registers the dispatcher + handlers
```

- `AddDDDBaseServices()` registers `IBaseCud<TContext,TEntity>` → `BaseCud<,>` and
  `IFluentCud<TContext,TEntity>` → `FluentCud<,>` as **scoped** services.
- `ConfigDDDEvents(assembly)` registers `IDomainEventDispatcher` (singleton) and scans the given
  assembly for all `IDomainEventHandler<T>` implementations.

Both `IBaseCud` and `IFluentCud` are resolved from the DI container and require your `DbContext`
to also be registered.

---

## 2. Domain Model

| Class | What it does | How to use |
|-------|--------------|------------|
| `BaseEntity` | Root of every persisted entity. Holds `EntityId Id`, `DateTime CreatedOn`, and `DateTime? UpdatedOn`. | Inherit your entities from it. |
| `EntityId` | Strongly-typed GUID identifier (`ValueObject<EntityId>`). Supports `==`/`!=` against `EntityId` and `Guid`, plus implicit `Guid → EntityId` and explicit `EntityId → Guid` casts. | `EntityId.CreateUniqueId()`, `EntityId.Create(guid)`, or just pass a `Guid` (implicit cast). |
| `ValueObject<T>` | Base class for value objects with structural equality (compares `GetEqualityComponents()`). | Inherit and implement `GetEqualityComponents()`. |
| `IAggregateRoot` | Marker + contract for aggregates: exposes `ICollection<IDomainEvent> Events` and `ClearEvents()`. | Implemented by `AggregateRootBase`. |
| `AggregateRootBase` | `BaseEntity` that is also an `IAggregateRoot`. Collects domain events via `AddEvent(...)` and exposes `Events` / `ClearEvents()`. | Inherit your aggregates from it; call `AddEvent(new MyEvent())` inside behavior methods. |
| `IDomainEvent` | Contract for a domain event. Carries `DateTime CreatedOn`. | Implement for each event type. |

```csharp
public class Order : AggregateRootBase
{
    public EntityId CustomerId { get; set; }

    public void Ship()
    {
        // ... business logic ...
        AddEvent(new OrderShippedEvent(Id)); // collected, dispatched on save
    }
}
```

---

## 3. Domain Events

| Class | What it does | How to use |
|-------|--------------|------------|
| `IDomainEventDispatcher` | Dispatches a collection of `IDomainEvent`s to their handlers. | Injected automatically; you normally don't call it directly. |
| `DomainEventDispatcher` | Default dispatcher. Resolves `IDomainEventHandler<T>` for each event from a **scoped** service provider and invokes `HandleAsync`. | Registered by `ConfigDDDEvents`. |
| `IDomainEventHandler<T>` | Handles a single event type. | Implement `HandleAsync(T domainEvent, CancellationToken ct)`. |
| `PipelineExtensions.BeforeSaveChanges` | Extension on `DbContext` that pulls pending events from `AggregateRootBase` entities in the change tracker, clears them, and dispatches them in one batch. | Call inside your `SaveChangesAsync` override (or before commit). |

**Event handler example**
```csharp
public class OrderShippedHandler : IDomainEventHandler<OrderShippedEvent>
{
    public Task HandleAsync(OrderShippedEvent domainEvent, CancellationToken ct = default)
    {
        // send email, publish to bus, etc.
        return Task.CompletedTask;
    }
}
```

> Tip: wire `PipelineExtensions.BeforeSaveChanges(dbContext, dispatcher, ct)` into your `DbContext.SaveChangesAsync`
> so aggregate events are dispatched automatically whenever EF persists changes.

---

## 4. Classic CUD — `IBaseCud` / `BaseCud`

`IBaseCud<TContext, TEntity>` is the non-fluent CUD contract; `BaseCud<,>` is its implementation.
Every mutating method **saves immediately** (unless a transaction is active — see §6).

**Methods**
- `Task<bool> InsertAsync(TEntity entity)` / `InsertAsync(IEnumerable<TEntity> entities)`
- `Task<bool> UpdateAsync(TEntity entity)` / `UpdateAsync(IEnumerable<TEntity> entities)`
- `Task<bool> DeleteAsync(object id)` / `DeleteAsync(TEntity entity)` / `DeleteAsync(IEnumerable<TEntity> entities)` / `DeleteAsync(Expression<Func<TEntity,bool>> where)`
- `Task<bool> SaveAsync(CancellationToken)` — flushes pending changes (no-op when a transaction is active).
- `Task<bool> BeginTransactionAsync(CancellationToken)` — starts a transaction (defers saves).
- `Task<bool> CommitAsync(CancellationToken)` — saves and commits the transaction.
- `Task RollbackAsync(CancellationToken)` — rolls back the transaction.
- Functional helpers:
  - `Task<TResult> InsertAsync<TResult>(Expression<Func<TEntity,bool>> existExpression, Func<TResult> exist, Func<TEntity> create, Func<TEntity?,TResult> final)`
  - `Task<TResult> InsertAsync<TResult>(Expression<Func<TEntity,bool>> existExpression, Func<TResult> exist, Func<TEntity> create, Func<TEntity?,Task<TResult>> final)`
  - `Task<TResult> UpdateAsync<TResult>(object id, Func<TResult> notFound, Func<TEntity?,Task<TResult>> final, Func<TEntity,TEntity> update)`
  - `Task<TResult> UpdateAsync<TResult>(object id, Func<TResult> notFound, Func<TEntity?,TResult> final, Func<TEntity,TEntity> update)`

**Usage**
```csharp
await cud.InsertAsync(order);
await cud.UpdateAsync(order);
await cud.DeleteAsync(orderId);

// "insert only if it doesn't already exist"
await cud.InsertAsync(
    existExpression: x => x.Code == code,
    exist: () => Results.Conflict(),
    create: () => new Order { Code = code },
    final: created => Results.Ok(created?.Id));
```

---

## 5. Fluent CUD — `IFluentCud` / `FluentCud`

`IFluentCud<TContext, TEntity>` is a **fluent** wrapper around `IBaseCud` that merges CUD operations
with **domain-event dispatching**. Each mutating call returns an `IFluentCudOperation` which is
**awaitable** (returns the CUD success `bool`) and can be chained with `DispatchEvent(...)` calls
that run *after* the CUD operation completes.

**Fluent operations** (all return `IFluentCudOperation<TContext,TEntity>`)
- `AddAsync(TEntity)` / `AddAsync(IEnumerable<TEntity>)`
- `UpdateAsync(TEntity)` / `UpdateAsync(IEnumerable<TEntity>)`
- `DeleteAsync(object id)` / `DeleteAsync(TEntity)` / `DeleteAsync(IEnumerable<TEntity>)` / `DeleteAsync(Expression<Func<TEntity,bool>> where)`

**`IFluentCudOperation` — `DispatchEvent` overloads**
- `DispatchEvent(bool condition, Func<IDomainEvent> whenTrue, Func<IDomainEvent> whenFalse)` — picks an event by condition (only when the CUD succeeded).
- `DispatchEventIf(bool condition, Func<IDomainEvent> event)` — dispatches only if the condition *and* the CUD succeeded.
- `DispatchEvent(Func<IDomainEvent> event)` — dispatches when the CUD succeeded.
- `DispatchEvent(Func<bool, IDomainEvent> onResult)` — picks an event from the actual CUD result (success/failure).
- `Task<bool> ExecuteAsync()` — runs the CUD + queued dispatches and returns the CUD success.

The operation is awaitable, so you can `await` it directly; chaining multiple `DispatchEvent` calls is supported.

**Usage**
```csharp
// Condition-based dispatch (true/false events)
await _cud.AddAsync(entity)
    .DispatchEvent(condition, () => new TrueEvent(), () => new FalseEvent());

// Only dispatch when the insert actually succeeded
await _cud.AddAsync(order)
    .DispatchEvent(() => new OrderCreatedEvent(order.Id));

// Conditional dispatch
await _cud.UpdateAsync(order)
    .DispatchEventIf(order.IsPaid, () => new OrderPaidEvent(order.Id));

// Choose event based on the real result
await _cud.DeleteAsync(id)
    .DispatchEvent(success => success ? new OrderDeletedEvent(id) : new DeleteFailedEvent(id));

// Chain multiple events
await _cud.AddAsync(order)
    .DispatchEvent(() => new OrderCreatedEvent(order.Id))
    .DispatchEventIf(order.IsPaid, () => new OrderPaidEvent(order.Id));
```

> Note: fluent `DispatchEvent` callbacks fire right after each operation (before any explicit
> `CommitAsync`). If you need events to dispatch only after a successful commit, combine the
> fluent API with a transaction (§6) and dispatch in the `onResult` overload.

---

## 6. Transactional Operations

Both `IBaseCud` and `IFluentCud` support explicit transactions **without any external state object**.
Internally they open an EF Core `IDbContextTransaction` and flip an `_autoSave` flag so that
individual operations are **deferred** until you commit.

**Flow**
1. `BeginTransactionAsync()` — opens a transaction and stops auto-saving after each operation.
2. Perform any number of `InsertAsync` / `UpdateAsync` / `DeleteAsync` (or fluent `AddAsync`/etc.).
3. `CommitAsync()` — saves everything in one `SaveChangesAsync` and commits the transaction.
   On failure it automatically rolls back.
4. `RollbackAsync()` — discards pending changes and ends the transaction.

```csharp
// Classic
await cud.BeginTransactionAsync();
await cud.InsertAsync(order);
await cud.InsertAsync(orderItem);
await cud.CommitAsync();   // one transaction; auto-rollback on failure

// Fluent
await _cud.BeginTransactionAsync();
await _cud.AddAsync(order).DispatchEvent(() => new OrderCreatedEvent(order.Id));
await _cud.AddAsync(orderItem);
await _cud.CommitAsync();
```

When no transaction is active, every operation saves immediately — existing behavior is unchanged.

---

## 7. Querying — Specifications & Extensions

| Class | What it does | How to use |
|-------|--------------|------------|
| `ISpecification<T>` | Contract describing a query: `Criteria`, `Includes`, `IncludeStrings`, `OrderBy`, `OrderByDescending`, `Skip`, `Take`, `IsPagingEnabled`. | Consumed by `SpecificationEvaluator`. |
| `BaseSpecification<T>` | Base class to build specifications. Provides `AddInclude`, `ApplyOrderBy`, `ApplyOrderByDescending`, `ApplyPaging`. | Inherit and configure in the constructor. |
| `SpecificationEvaluator` | Applies a specification to an `IQueryable<T>` (where + includes + order + paging). | `SpecificationEvaluator.GetQuery(query, spec)`. |
| `QueryExtension` | Static LINQ helpers: `ToPagination`, `ToPaginationAsync`, `WhereIf`, `OrderByIf`, `WhereContains`, `WhereIn`. | Call on any `IQueryable<T>`. |

**Specification example**
```csharp
public class ActiveOrdersSpec : BaseSpecification<Order>
{
    public ActiveOrdersSpec()
    {
        Criteria = o => !o.IsCancelled;
        AddInclude(o => o.Items);
        ApplyOrderByDescending(o => o.CreatedOn);
        ApplyPaging(0, 20);
    }
}

var query = SpecificationEvaluator.GetQuery(dbContext.Set<Order>(), new ActiveOrdersSpec());
```

**Query extensions**
```csharp
var result = await dbContext.Set<Order>()
    .WhereIf(!string.IsNullOrEmpty(status), o => o.Status == status)
    .WhereContains(o => o.Code, search)
    .WhereIn(o => o.CustomerId, customerIds)
    .OrderByIf(sortByDate, o => o.CreatedOn, descending: true)
    .ToPaginationAsync(page, pageSize);
```

---

## 8. Pagination

| Class | What it does | How to use |
|-------|--------------|------------|
| `PaginationResult<TResult>` | Record holding `PageCount`, `ItemsCount`, and `Items`. | Returned by pagination helpers. |
| `PaginationExtension` | Maps/projects a `PaginationResult` and computes `PageCount`. | `res.FromPagination(mappedItems)`, `items.ToPagination(count, pageSize)`. |

```csharp
PaginationResult<Order> page = await query.ToPaginationAsync(pageIndex, pageSize);
PaginationResult<OrderDto> dto = page.FromPagination(page.Items.Select(ToDto));
```

---

## 9. Dynamic DTO Builder

Builds dictionaries (or runtime types for Swagger) from aggregates, with support for ignoring
properties, conditional ignores, and computed/extra properties.

| Class | What it does | How to use |
|-------|--------------|------------|
| `DtoBuilder<T>` | Fluent builder for an `AggregateRootBase` `T`. Methods: `Ignore`, `IgnoreWhere`, `Add`, `ApplyProfile`, `ApplyProfilesFromRegistry`, `Build`, `BuildList`, `BuildType`, `BuildTypeList`. | Start from `aggregate.ToDtoBuilder()`. |
| `DtoFactory` | Extension `ToDtoBuilder<T>(this T aggregate)` that creates a `DtoBuilder<T>`. | `var dto = order.ToDtoBuilder().Ignore(o => o.Secret).Build();` |
| `DtoProfile<T>` | Abstract profile that configures a `DtoBuilder<T>` via `Configure`. | Inherit and register. |
| `DtoProfileRegistry` | Stores/looks up `DtoProfile<T>` instances; can scan an assembly. | `RegisterProfile(...)`, `RegisterProfilesFromAssembly(...)`, `GetProfilesFor<T>()`. |
| `TypeBuilderExtensions` | IL helper that emits a property + backing field on a `TypeBuilder` (used by `BuildType`). | Internal; not called directly. |

```csharp
var dto = order.ToDtoBuilder()
    .Ignore(o => o.InternalNotes)
    .IgnoreWhere(o => o.Status, o => o.Status == "Draft")
    .Add("Total", o => o.Items.Sum(i => i.Price))
    .Build();   // => Dictionary<string, object?>

// With a profile
public class OrderProfile : DtoProfile<Order>
{
    public override void Configure(DtoBuilder<Order> b)
        => b.Ignore(o => o.InternalNotes).Add("Total", o => o.Items.Sum(i => i.Price));
}
order.ToDtoBuilder().ApplyProfile(new OrderProfile()).Build();
```

---

## 10. Class Reference

### Domain (`DDD.BaseModels`)
- `BaseEntity` — base persisted entity (`Id`, `CreatedOn`, `UpdatedOn`).
- `EntityId` — strongly-typed GUID id with `Guid` interop.
- `ValueObject<T>` — structural-equality base for value objects.
- `IAggregateRoot` — aggregate contract (`Events`, `ClearEvents`).
- `AggregateRootBase` — `BaseEntity` + `IAggregateRoot` with `AddEvent`/`Events`/`ClearEvents`.
- `IDomainEvent` — domain event contract (`CreatedOn`).
- `IDomainEventHandler<T>` — handles one event type.
- `IDomainEventDispatcher` / `DomainEventDispatcher` — dispatches events to handlers.
- `PipelineExtensions` — `BeforeSaveChanges` dispatches aggregate events before commit.

### CUD (`DDD.BaseModels.Service`)
- `IBaseCud<TContext,TEntity>` / `BaseCud<TContext,TEntity>` — classic CUD + transactions.
- `IFluentCud<TContext,TEntity>` / `FluentCud<TContext,TEntity>` — fluent CUD + event dispatch + transactions.
- `IFluentCudOperation<TContext,TEntity>` / `FluentCudOperation<TContext,TEntity>` — awaitable, chainable operation result with `DispatchEvent` overloads.

### Querying (`DDD.BaseModels.Service`)
- `ISpecification<T>` / `BaseSpecification<T>` — specification pattern.
- `SpecificationEvaluator` — applies a specification to an `IQueryable`.
- `QueryExtension` — `ToPagination`, `ToPaginationAsync`, `WhereIf`, `OrderByIf`, `WhereContains`, `WhereIn`.
- `PaginationResult<TResult>` / `PaginationExtension` — pagination result + mapping helpers.

### DTO (`DDD.BaseModels.Service`)
- `DtoBuilder<T>` / `DtoFactory` — dynamic DTO construction.
- `DtoProfile<T>` / `DtoProfileRegistry` — reusable DTO profiles.
- `TypeBuilderExtensions` — runtime type emission helper.

### Configuration (`DDD.BaseModels`)
- `DddConfig.AddDDDBaseServices()` — registers CUD services.
- `DddConfig.ConfigDDDEvents(assembly)` — registers the event dispatcher and handlers.

---

## Notes & Conventions
- All CUD services are **scoped**; resolve them per request.
- `EntityId` is implicitly convertible from `Guid`, so you can pass a `Guid` wherever an `EntityId` is expected.
- Domain events are dispatched through the registered `IDomainEventDispatcher`; use
  `PipelineExtensions.BeforeSaveChanges` to automate dispatch on save.
- Transactions are first-class and require no external Unit-of-Work or repository-state type.
