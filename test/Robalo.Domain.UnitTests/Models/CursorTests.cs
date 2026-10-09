using Robalo.Domain.Models;

namespace Robalo.Domain.UnitTests.Models;

public class CursorTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Create_ShouldThrow_OnInvalidLimit()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Cursor.Create(limit: 0, reference: 1, cursorDirection: _fixture.Create<CursorDirection>()));
    }

    [Fact]
    public void Create_ShouldThrow_OnInvalidReference()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Cursor.Create(limit: 1, reference: 0, cursorDirection: _fixture.Create<CursorDirection>()));
    }

    [Fact]
    public void Create_ShouldThrow_OnInvalidDirection()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Cursor.Create(limit: 1, reference: 1, (CursorDirection)55));
    }

    [Fact]
    public void Create_ShouldCreateValidCursor()
    {
        var limit = _fixture.Create<int>();
        var reference = _fixture.Create<int>();
        var direction = _fixture.Create<CursorDirection>();
        var cursor = Cursor.Create(limit: limit, reference: reference, direction);

        cursor.Limit.ShouldBe(limit);
        cursor.Reference.ShouldBe(reference);
        cursor.Direction.ShouldBe(direction);
        cursor.CursorString.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FromCursorString_ShouldRestoreValidCursor()
    {
        var cursor = Cursor.Create(_fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<CursorDirection>());

        var cursorNew = Cursor.FromCursorString(cursor.CursorString);
        cursorNew.CursorString.ShouldBe(cursor.CursorString);
        cursorNew.Limit.ShouldBe(cursor.Limit);
        cursorNew.Reference.ShouldBe(cursor.Reference);
        cursorNew.Direction.ShouldBe(cursor.Direction);

        cursorNew.ShouldBe(cursor);    
    }

    [Fact]
    public void FromCursorString_ShouldThrow_On_InvalidString()
    {
        Should.Throw<ArgumentException>(() => Cursor.FromCursorString(_fixture.Create<string>()));
    }

}