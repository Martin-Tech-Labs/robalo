namespace Robalo.Common.UnitTests;

public class Test
{
    private readonly Fixture _fixture = new();
    
    [Fact]
    public void When_testing_something_2()
    {
        var testString = _fixture.Create<string>();
        testString = "";
        testString.ShouldNotBeNullOrWhiteSpace();
    }
}