using Robalo.Common.Models;

namespace Robalo.Common.UnitTests.Models;

public class OptionTests
{
    private record TestRecord(string Field1, int Field2, bool Field3);
    private readonly Fixture _fixture = new();

    [Fact]
    public void ToString_ForNone_ShouldProduce_None()
    {
        Option<int> option = None.Default;
        option.ToString().ShouldBe("None");
    }

    [Fact]
    public void ToString_ForOptionInt_ShouldProduce_ExpectedOutput()
    {
        var number = _fixture.Create<int>();
        Option<object> option = number;
        option.ToString().ShouldBe($"Some({number})");
    }

    [Fact]
    public void Equals_TwoNoneOfSameOptionTypeShouldEqual()
    {
        Option<TestRecord> left = None.Default;
        Option<TestRecord> right = None.Default;

        left.ShouldBe(right);
        left.Equals(right).ShouldBeTrue();

        (left == right).ShouldBeTrue();
        (left != right).ShouldBeFalse();
    }

    [Fact]
    public void Equals_TwoNoneOfDifferentOptionType_ShouldNotEqual()
    {
        Option<TestRecord> left = None.Default;
        Option<int> right = None.Default;

        left.ShouldNotBe((object)right);
        left.Equals(right).ShouldBeFalse();
    }

    [Fact]
    public void Equals_SomeAndNone_ShouldNotEqual()
    {
        Option<int> left = None.Default;
        Option<int> right = _fixture.Create<int>();

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);

        left.Equals(right).ShouldBeFalse();
        right.Equals(left).ShouldBeFalse();

        (left == right).ShouldBeFalse();
        (right == left).ShouldBeFalse();

        (left != right).ShouldBeTrue();
        (right != left).ShouldBeTrue();
    }

    [Fact]
    public void Equals_TwoSameSome_ShouldEqual()
    {
        var value1 = _fixture.Create<string>();
        var value2 = _fixture.Create<int>();
        var value3 = _fixture.Create<bool>();

        Option<TestRecord> left = new TestRecord(value1, value2, value3);
        Option<TestRecord> right = new TestRecord(value1, value2, value3);

        left.ShouldBe(right);
        left.Equals(right).ShouldBeTrue();

        (left == right).ShouldBeTrue();
        (left != right).ShouldBeFalse();
    }

    [Fact]
    public void Equals_TwoDifferentSome_ShouldNotEqual()
    {
        var value1 = _fixture.Create<string>();
        var value2 = _fixture.Create<int>();
        var value3 = _fixture.Create<bool>();

        var record = new TestRecord(value1, value2, value3);

        Option<TestRecord> left = record;
        Option<TestRecord> right = record with { Field1 = _fixture.Create<string>() };

        left.ShouldNotBe(right);
        left.Equals(right).ShouldBeFalse();

        (left == right).ShouldBeFalse();
        (left != right).ShouldBeTrue();
    }

    [Fact]
    public void Equals_SomeAndSameValue_ShouldEqual()
    {
        var value1 = _fixture.Create<string>();
        var value2 = _fixture.Create<int>();
        var value3 = _fixture.Create<bool>();

        TestRecord left = new(value1, value2, value3);
        Option<TestRecord> right = new TestRecord(value1, value2, value3);

        (left == right).ShouldBeTrue();
        (left != right).ShouldBeFalse();

        (right == left).ShouldBeTrue();
        (right != left).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DefaultAndNone_ShouldEqual()
    {
        Option<int> left = None.Default;
        Option<int> right = default;

        left.ShouldBe(right);
        right.ShouldBe(left);

        left.Equals(right).ShouldBeTrue();
        right.Equals(left).ShouldBeTrue();

        (left == right).ShouldBeTrue();
        (right == left).ShouldBeTrue();

        (left != right).ShouldBeFalse();
        (right != left).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DefaultAndDefault_ShouldEqual()
    {
        Option<int> left = default;
        Option<int> right = default;

        left.ShouldBe(right);

        left.Equals(right).ShouldBeTrue();

        (left == right).ShouldBeTrue();

        (left != right).ShouldBeFalse();
    }
}