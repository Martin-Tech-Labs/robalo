namespace Robalo.Common.Extensions;

public static class TaskExtensions
{
#pragma warning disable CC001
    public static async Task<List<T>> ToList<T>(this Task<IEnumerable<T>> task)
    {
        return (await task).ToList();
    }
#pragma warning restore
}