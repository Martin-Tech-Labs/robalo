using Robalo.Controller.Api.Requests;
using Robalo.Controller.Api.Validation.Requests;

namespace Robalo.Controller.UnitTests.Validation.Requests;

public class MessageApiRequestValidatorTests
{
    private readonly MessageApiRequestValidator _sut = new();
    private readonly Fixture _fixture = new();

    [Theory]
    [InlineData("", "content_must_be_provided")]
    [InlineData("   ", "content_must_be_provided")]
    [InlineData(null, "content_must_be_provided")]
    public void Validate_ShouldReturn_ExpectedError(string? content, string error)
    {
        var request = new MessageApiRequest(content);
        var result = _sut.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.Single().ErrorMessage.ShouldBe(error);
    }

    [Fact]
    public void Validate_ShouldReturn_ExpectedError_On_Content_Max_Length_Exceeded()
    {
        var request = new MessageApiRequest(new string('a', 101));
        var result = _sut.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.Single().ErrorMessage.ShouldBe("content_must_not_exceed_100_characters");
    }

    [Fact]
    public void Validate_ShouldReturn_Valid_On_Content_Max_Length()
    {
        var request = new MessageApiRequest(new string('a', 100));
        var result = _sut.Validate(request);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ShouldReturn_Valid_On_Normal_Content()
    {
        var request = new MessageApiRequest(_fixture.Create<string>());
        var result = _sut.Validate(request);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}