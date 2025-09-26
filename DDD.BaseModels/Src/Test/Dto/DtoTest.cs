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
}

class Domain : AggregateRootBase
{
    public string Name { get; set; } = null!;

    public string LastName { get; set; } = null!;
}