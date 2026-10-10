namespace Robalo.Common.Services;
public class DateTimeOffsetProvider : IDateTimeOffsetProvider
{

}

public interface IDateTimeOffsetProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}