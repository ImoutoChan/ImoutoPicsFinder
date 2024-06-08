using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImoutoPicsFinder;

public static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var list = new List<T>();
        
        await foreach (var item in source)
            list.Add(item);

        return list;
    }
}
