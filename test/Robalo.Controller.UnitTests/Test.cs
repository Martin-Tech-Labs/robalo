namespace Robalo.Controller.UnitTests;

public class Test
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public void Test1()
    {
        var testString = _fixture.Create<string>();
        testString.ShouldNotBeNullOrWhiteSpace();
    }
}
