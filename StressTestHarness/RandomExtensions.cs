using System.Diagnostics.CodeAnalysis;

namespace StressTestHarness;

public static class RandomExtensions
{
    public static T? RandomItemInList<T>(this Random r, List<T> list)
    {
        return list.Count == 0 ? default : list[r.Next(0, list.Count)];
    }
}