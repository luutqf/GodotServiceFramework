namespace GodotServiceFramework.Extensions;

public static class CollectionExtensions
{
    /// <summary>
    /// A convience method for a foreach loop at the the sacrafice of debugging support
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> value, Action<T> action)
    {
        foreach (T element in value)
        {
            action(element);
        }
    }
}