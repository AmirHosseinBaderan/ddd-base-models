using DDD.BaseModels;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Test.ValueObject;

public class ValueObject(ITestOutputHelper outputHelper)
{
    [Fact]
    public void ConvertToValueObject()
    {
        VoDto dto = new("test");
        var id = EntityId.CreateUniqueId();
        var obj = dto.ToValueObject<VoTest, VoDto>(x => x.Id = id);

        outputHelper.WriteLine(JsonConvert.SerializeObject(obj));

        Assert.True(obj.Id == id && obj.Name == dto.Name);
    }
}

public class VoTest : ValueObject<VoTest>
{
    public EntityId Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public override IEnumerable<object> GetEqualityComponents()
        => [Id, Name];
}

public record VoDto(string Name);