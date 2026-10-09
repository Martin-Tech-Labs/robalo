using Robalo.Domain.Models;
using Robalo.Tests.Common;

namespace Robalo.Domain.UnitTests.Models;

public class CursorTests
{
    private readonly Fixture _fixture = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11_000)]
    public void Create_ShouldThrow_OnInvalidLimit(int limit)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Cursor.Create(limit: limit, reference: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11_000)]
    public void Create_ShouldThrow_OnInvalidLimit_ForCursorLast(int limit)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Cursor.Last(limit: limit));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11_000)]
    public void Create_ShouldThrow_OnInvalidLimit_ForCursorFirst(int limit)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Cursor.First(limit: limit));
    }

    [Fact]
    public void Create_ShouldThrow_OnInvalidReference()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Cursor.Create(limit: 1, reference: -1));
    }

    [Fact]
    public void Create_ShouldCreateValidCursor()
    {
        var limit = _fixture.Create<int>();
        var reference = _fixture.Create<int>();
        var cursor = Cursor.Create(limit: limit, reference: reference);

        cursor.Limit.ShouldBe(limit);
        cursor.Reference.ShouldBe(reference);
        cursor.CursorString.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_ShouldCreateValidCursor_WithCursorLast()
    {
        var limit = _fixture.Create<int>();
        var cursor = Cursor.Last(limit: limit);

        cursor.Limit.ShouldBe(limit);
        cursor.Reference.ShouldBeNone();
        cursor.CursorString.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_ShouldCreateValidCursor_WithCursorFirst()
    {
        var limit = _fixture.Create<int>();
        var cursor = Cursor.First(limit: limit);

        cursor.Limit.ShouldBe(limit);
        cursor.Reference.ShouldBe(1);
        cursor.CursorString.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FromCursorString_ShouldRestoreValidCursor()
    {
        var cursor = Cursor.Create(_fixture.Create<int>(), _fixture.Create<int>());

        var cursorNew = Cursor.FromCursorString(cursor.CursorString);
        cursorNew.CursorString.ShouldBe(cursor.CursorString);
        cursorNew.Limit.ShouldBe(cursor.Limit);
        cursorNew.Reference.ShouldBe(cursor.Reference);

        cursorNew.ShouldBe(cursor);
    }

    [Fact]
    public void FromCursorString_ShouldRestoreValidCursor_WithCursorLast()
    {
        var cursor = Cursor.Last(_fixture.Create<int>());

        var cursorNew = Cursor.FromCursorString(cursor.CursorString);
        cursorNew.CursorString.ShouldBe(cursor.CursorString);
        cursorNew.Limit.ShouldBe(cursor.Limit);
        cursorNew.Reference.ShouldBe(cursor.Reference);

        cursorNew.ShouldBe(cursor);
    }

    [Fact]
    public void FromCursorString_ShouldRestoreValidCursor_WithCursorFirst()
    {
        var cursor = Cursor.First(_fixture.Create<int>());

        var cursorNew = Cursor.FromCursorString(cursor.CursorString);
        cursorNew.CursorString.ShouldBe(cursor.CursorString);
        cursorNew.Limit.ShouldBe(cursor.Limit);
        cursorNew.Reference.ShouldBe(cursor.Reference);

        cursorNew.ShouldBe(cursor);
    }

    [Fact]
    public void FromCursorString_ShouldThrow_On_InvalidString()
    {
        Should.Throw<ArgumentException>(() => Cursor.FromCursorString(_fixture.Create<string>()));
    }

}