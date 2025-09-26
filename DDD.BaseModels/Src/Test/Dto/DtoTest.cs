using DDD.BaseModels;
using DDD.BaseModels.Service;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Test;

public class DtoTest(ITestOutputHelper outputHelper)
{
    [Fact]
    public void TestDtoBuilder()
    {
        DateTime now = DateTime.Now;
        Domain domain = new()
        {
            Id = EntityId.CreateUniqueId(),
            Name = "amir",
            LastName = "baderan",
            CreatedOn = now,
        };

        var dto = domain
            .ToDtoBuilder()
            .Ignore(x => x.Events)
            .IgnoreWhere(x => x.UpdatedOn, x => x.UpdatedOn == null)
            .Add("TestAdd", x => "value test")
            .Build();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(domain.Id.Value, dto["Id"]);
        Assert.Equal("amir", dto["Name"]);
        Assert.Equal("baderan", dto["LastName"]);
        Assert.Equal(now, dto["CreatedOn"]);
        Assert.Equal("value test", dto["TestAdd"]);

        outputHelper.WriteLine("Input model is {0} and Dto is {1}", JsonConvert.SerializeObject(domain),
            JsonConvert.SerializeObject(dto));
    }
    
    [Fact]
    public void TestDtoTypeBuilder()
    {
        DateTime now = DateTime.Now;
        Domain domain = new()
        {
            Id = EntityId.CreateUniqueId(),
            Name = "amir",
            LastName = "baderan",
            CreatedOn = now,
        };

        // Build dynamic DTO type
        var dtoType = domain
            .ToDtoBuilder()
            .Ignore(x => x.Events)
            .IgnoreWhere(x => x.UpdatedOn, x => x.UpdatedOn == null)
            .Add("TestAdd", x => "value test")
            .BuildType("DomainDto");

        // Create instance of the dynamic type
        var dtoInstance = Activator.CreateInstance(dtoType)!;

        // Copy properties from aggregate to DTO
        foreach (var prop in dtoType.GetProperties())
        {
            object? value = null;

            if (prop.Name == "Id")
                value = domain.Id.Value;
            else if (prop.Name == "Name")
                value = domain.Name;
            else if (prop.Name == "LastName")
                value = domain.LastName;
            else if (prop.Name == "CreatedOn")
                value = domain.CreatedOn;
            else if (prop.Name == "TestAdd")
                value = "value test";

            prop.SetValue(dtoInstance, value);
        }

        // Serialize input and output for verification
        outputHelper.WriteLine("Input model: {0}", JsonConvert.SerializeObject(domain));
        outputHelper.WriteLine("Dto object : {0}", JsonConvert.SerializeObject(dtoInstance));

        // Assert basic checks
        var dtoJson = JsonConvert.SerializeObject(dtoInstance);
        Assert.Contains("amir", dtoJson);
        Assert.Contains("value test", dtoJson);
        Assert.DoesNotContain("Events", dtoJson); // Ignored
    }
    
    [Fact]
    public void ApplyProfiles_FromAssembly_Works()
    {
        // Arrange
        var domain = new Domain
        {
            Id = EntityId.CreateUniqueId(),
            Name = "Amir",
            LastName = "Baderan",
            CreatedOn = DateTime.UtcNow
        };

        // Register all profiles from this test assembly
        DtoProfileRegistry.RegisterProfilesFromAssembly(typeof(DomainProfile).Assembly);

        // Act
        var dto = domain
            .ToDtoBuilder()
            .ApplyProfilesFromRegistry()
            .Build();

        var json = JsonConvert.SerializeObject(dto);
        outputHelper.WriteLine("DTO JSON: " + json);

        // Assert
        Assert.Contains("Amir", json); // original Name
        Assert.Contains("value test", json); // added by profile
        Assert.DoesNotContain("Events", json); // ignored by profile
    }
}

class Domain : AggregateRootBase
{
    public string Name { get; set; } = null!;

    public string LastName { get; set; } = null!;
}

class DomainProfile : DtoProfile<Domain>
{
    public override void Configure(DtoBuilder<Domain> builder)
    {
        builder
            .Ignore(x => x.Events)
            .IgnoreWhere(x => x.UpdatedOn, x => x.UpdatedOn == null)
            .Add("TestAdd", x => "value test");
    }
}