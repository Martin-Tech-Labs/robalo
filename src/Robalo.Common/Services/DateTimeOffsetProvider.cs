namespace Robalo.Common.Services;
public class DateTimeOffsetProvider : IDateTimeOffsetProvider
{

}

public interface IDateTimeOffsetProvider
{
    public DateTimeOffset UtcKnow => DateTimeOffset.UtcNow;
}