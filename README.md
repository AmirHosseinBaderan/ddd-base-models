# DDD.BaseModels

A lightweight base library for building **Domain-Driven Design (DDD)** projects in C#.
Provides **base entities, aggregates, value objects, and services** to reduce boilerplate and keep your domain clean.

---

## 📦 Installation

Install via **NuGet Package Manager**:

```powershell
dotnet add package DDD.BaseModels
```

Or via Visual Studio:

```
PM> Install-Package DDD.BaseModels
```

---

## 🚀 Features

* `EntityId` – strongly typed entity identifiers
* `BaseEntity` – common base for all entities
* `AggregateRootBase` – aggregate root with identity tracking
* `ValueObject<T>` – helper for implementing value objects
* `DtoBuilder<T>` – fluent API to build DTOs from aggregates
* `IBaseCud<TContext, TEntity>` – base Create/Update/Delete service contracts
* **Specification Pattern** – flexible query specifications (`ISpecification<T>`, `BaseSpecification<T>`, `SpecificationEvaluator`)
* **Query Extensions** – LINQ helpers for pagination, filtering, and conditional queries

---

## 🛠️ Usage

### 1. Create Your Aggregate

```csharp
using DDD.BaseModels;

public class User : AggregateRootBase
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
}
```

---

### 2. Working with EntityId

```csharp
var user = new User
{
    Id = EntityId.CreateUniqueId(),
    Name = "Amir",
    Email = "amir@example.com"
};

Console.WriteLine(user.Id); // strongly typed GUID wrapper
```

---

### 3. Build DTOs with `DtoBuilder`

```csharp
using DDD.BaseModels.Service;

var dto = user.ToDtoBuilder()
              .Ignore(u => u.Email)                 // ignore property
              .Add("IsActive", u => true)           // add custom field
              .Add("RegisteredAt", _ => DateTime.UtcNow)
              .Build();
```

Result (`Dictionary<string, object?>`):

```json
{
  "Name": "Amir",
  "Id": "d4caa5b6-46b1-4f2e-8d1c-bef14dbf6672",
  "IsActive": true,
  "RegisteredAt": "2025-09-25T12:34:56Z"
}
```

---

### 4. Conditional Ignore with `IgnoreWhere`

```csharp
var dto = user.ToDtoBuilder()
              .IgnoreWhere(u => u.Email, u => string.IsNullOrEmpty(u.Email))
              .Build();
```

If `Email` is `null` or empty, it will be excluded from the DTO.

---

### 5. Base CUD Service

```csharp
using DDD.BaseModels.Service;

public class AppDbContext : DbContext { }

public class UserService(IBaseCud<AppDbContext, User> cud)
{
    private readonly IBaseCud<AppDbContext, User> _cud = cud;

    public async Task AddUser(User user) =>
        await _cud.InsertAsync(user);
}
```

---

### 6. Specification Pattern

#### Define a Specification

```csharp
using DDD.BaseModels.Service;
using System.Linq.Expressions;

public class ActiveUsersSpecification : BaseSpecification<User>
{
    public ActiveUsersSpecification()
        : base(u => u.IsActive)
    {
        ApplyOrderBy(u => u.Name);
        ApplyPaging(0, 20);
    }
}
```

#### Apply Specification with Evaluator

```csharp
var activeUsersSpec = new ActiveUsersSpecification();
var query = SpecificationEvaluator.GetQuery(dbContext.Users.AsQueryable(), activeUsersSpec);
var users = await query.ToListAsync();
```

---

### 7. Query Extensions

#### Pagination

```csharp
var pagedUsers = dbContext.Users
                           .ToPagination(page: 1, pageSize: 10)
                           .ToList();
```

#### Conditional Filtering

```csharp
var filtered = dbContext.Users
                        .WhereIf(includeInactive, u => !u.IsActive)
                        .OrderByIf(sortByName, u => u.Name)
                        .ToList();
```

#### Where Contains & Where In

```csharp
var searchResult = dbContext.Users
                            .WhereContains(u => u.Name, "Amir")
                            .WhereIn(u => u.Id, allowedIds)
                            .ToList();
```

---

### 8. Dependency Injection Extensions

```csharp
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Register DDD services
services.AddDDDBaseServices();

// Configure domain events from current assembly
services.ConfigDDDEvents(typeof(Program).Assembly);
```
