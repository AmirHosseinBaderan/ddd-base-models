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
}

class Domain : AggregateRootBase
{
    public string Name { get; set; } = null!;

    public string LastName { get; set; } = null!;
}